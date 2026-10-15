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
           .Set(CatWeaponState.FromConfig(pCfg))
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

    /// <summary>武器クールダウンの残り秒数を取得または設定します。</summary>
    public float ShootCooldown
    {
        get => CurrentCharacter == CharacterType.Bunny
            ? (Entity.Has<BunnyWeaponState>() ? Entity.Get<BunnyWeaponState>().CooldownRemaining : 0.0f)
            : (Entity.Has<CatWeaponState>() ? Entity.Get<CatWeaponState>().CooldownRemaining : 0.0f);
        set
        {
            var val = Math.Max(0.0f, value);
            if (CurrentCharacter == CharacterType.Bunny && Entity.Has<BunnyWeaponState>())
            {
                Entity.GetMut<BunnyWeaponState>().CooldownRemaining = val;
            }
            else if (Entity.Has<CatWeaponState>())
            {
                Entity.GetMut<CatWeaponState>().CooldownRemaining = val;
            }
        }
    }

    /// <summary>現在選択されているキャラクター種別を取得します。</summary>
    public CharacterType CurrentCharacter { get; private set; } = CharacterType.Cat;

    /// <summary>
    ///     自機キャラクター（Cat または Bunny）を動的に切り替えます。
    ///     スプライト画像ハンドル、アニメーション設定および武器状態を同期更新します。
    /// </summary>
    /// <param name="newChar">切り替え先のキャラクター種別。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    public void SwitchCharacter(CharacterType newChar, PlayerConfig pCfg)
    {
        CurrentCharacter = newChar;
        ref var anim = ref Entity.GetMut<Animation>();
        anim.CharacterType = newChar;
        anim.Play("Idle");

        if (AssetManager.TryGetCharacterSpriteHandle(newChar, "Idle", out var handle))
        {
            ref var sprite = ref Entity.GetMut<Sprite>();
            sprite.GraphHandle = handle;
            sprite.SrcRect = new Rect(0, 0, 32, 32);
        }

        Entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        Entity.Set(new Hurtbox(pCfg.GetHurtboxShape()));
        Entity.Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight));

        if (Entity.Has<Health>())
        {
            ref var hp = ref Entity.GetMut<Health>();
            var oldMax = hp.Max;
            hp.Max = pCfg.MaxHp;
            if (hp.Current >= oldMax)
            {
                hp.Current = pCfg.MaxHp;
            }
            else
            {
                hp.Current = Math.Clamp(hp.Current, 0.0f, hp.Max);
            }
        }
        else
        {
            Entity.Set(new Health(pCfg.MaxHp));
        }

        if (Entity.Has<Transform>())
        {
            ref var t = ref Entity.GetMut<Transform>();
            t.ScaleX = pCfg.ScaleX;
            t.ScaleY = pCfg.ScaleY;
        }

        if (newChar == CharacterType.Bunny)
        {
            Entity.Remove<CatWeaponState>();
            Entity.Set(BunnyWeaponState.FromConfig(pCfg));
        }
        else
        {
            Entity.Remove<BunnyWeaponState>();
            Entity.Set(CatWeaponState.FromConfig(pCfg));
        }

        Debug.WriteLine($"[PlayerPawn] プレイヤーキャラクターを切り替えました: {newChar} (Speed: {pCfg.MoveSpeed}, Jump: {pCfg.FirstJumpVelocity})");
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

        Entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        SwitchCharacter(CurrentCharacter, pCfg);
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
