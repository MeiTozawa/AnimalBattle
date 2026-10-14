using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     プレイヤー自機の移動、跳躍、重力、射撃およびアニメーション状態を一元管理する受控躯体（PlayerPawn）です。
///     <see cref="Pawn" /> 基底クラスを継承し、空間・物理・生命・描画の共通操作を利用します。
/// </summary>
public sealed class PlayerPawn : Pawn
{
    /// <summary>プレイヤー自機のアビリティシステムコンポーネントを取得します。</summary>
    public override AbilitySystemComponent AbilitySystem { get; } = new();

    /// <summary>
    ///     設定情報と Flecs エンティティ句柄を指定して、自機 Pawn を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライトの画像ハンドル。</param>
    public PlayerPawn(Entity entity, PlayerConfig pCfg, int spriteHandle) : base(entity)
    {
        Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");

        Entity.Set(new Health(pCfg.MaxHp))
           .Set(new InvincibleOnHit(pCfg.InvincibleDuration))
           .Set(new HitFlashSettings(pCfg.HitFlashDuration, Color.White))
           .Set(new Transform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY))
           .Set(new Velocity(0.0f))
           .Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight))
           .Set(new Hurtbox(pCfg.GetHurtboxShape()))
           .Set(new Animation("Idle"))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 32, 32)))
           .Set(new MovementIntent())
           .Set(new WeaponIntent())
           .Set(JumpCapability.FromPlayerConfig(pCfg))
           .Set(WeaponState.FromPlayerConfig(pCfg))
           .Set(new SpringScale(1.0f, 1.0f, pCfg.SpringFrequency, pCfg.SpringDampingRatio))
           .Set(new DashState())
           .Set(AbilitySystem)
           .Add<PlayerTag>();
    }

    /// <summary>スプリング拡縮変形状態の参照を取得します（常駐 SpringScale への直接アクセス）。</summary>
    public ref readonly SpringScale SpringScale => ref Entity.Get<SpringScale>();

    /// <summary>現在ダッシュ中であるかどうかを取得します。</summary>
    public bool IsDashing => Entity.Get<DashState>().IsDashing;

    /// <summary>ダッシュ再使用クールダウン残り秒数を取得します。</summary>
    public float DashCooldown => Entity.Get<DashState>().Cooldown;

    /// <summary>残り二段ジャンプ回数を取得または設定します。</summary>
    public int RemainingJumps
    {
        get => Entity.Get<JumpCapability>().RemainingJumps;
        set => Entity.GetMut<JumpCapability>().RemainingJumps = value;
    }

    /// <summary>射撃クールダウンの残り秒数を取得または設定します。</summary>
    public float ShootCooldown
    {
        get => Entity.Get<WeaponState>().CooldownRemaining;
        set => Entity.GetMut<WeaponState>().CooldownRemaining = Math.Max(0.0f, value);
    }

    /// <summary>
    ///     自機 Pawn を指定設定で初期位置へリセットします。
    /// </summary>
    /// <param name="pCfg">リセット用プレイヤー設定。</param>
    public void Reset(PlayerConfig pCfg)
    {
        Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");

        SetTransform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY);
        SetVelocity(0.0f, 0.0f);
        ResetHealth(pCfg.MaxHp);

        ref var c = ref Entity.GetMut<CollisionBox>();
        c.IsGrounded = false;
        c.IsOnWall = false;

        Entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        Entity.Set(WeaponState.FromPlayerConfig(pCfg));
        Entity.Set(new SpringScale(1.0f, 1.0f, pCfg.SpringFrequency, pCfg.SpringDampingRatio));
        Entity.Set(new DashState());
        Entity.GetMut<MovementIntent>().Reset();
        Entity.GetMut<WeaponIntent>().Reset();

        PlayAnimation("Idle");
        FlipX = false;
        IsVisible = true;
        ClearTransientEffects();
        AbilitySystem.ClearTags();
    }
}
