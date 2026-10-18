using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     プレイヤー自機の移動、跳躍、重力、射撃およびアニメーション状態を一元管理する受控躯体（PlayerPawn）の抽象基底クラスです。
///     <see cref="Pawn" /> 基底クラスを継承し、空間・物理・生命・描画の共通操作を提供します。
/// </summary>
public abstract class PlayerPawn : Pawn
{
    /// <summary>現在選択されているキャラクター種別を取得します。</summary>
    public CharacterType CharacterType { get; }

    /// <summary>後方互換性用のキャラクター種別プロパティ。</summary>
    public CharacterType CurrentCharacter => CharacterType;

    /// <summary>スプリング拡縮変形状態の参照を取得します（常駐 SpringScale への直接アクセス）。</summary>
    public ref readonly SpringScale SpringScale => ref Entity.Get<SpringScale>();

    /// <summary>ダッシュ状態コンポーネントの読み取り専用参照を取得します。</summary>
    public ref readonly DashState Dash => ref Entity.Get<DashState>();

    /// <summary>現在ダッシュ中であるかどうかを取得します。</summary>
    public bool IsDashing => Dash.IsDashing;

    /// <summary>ダッシュ再使用クールダウン残り秒数を取得します。</summary>
    public float DashCooldown => Dash.Cooldown;

    /// <summary>跳躍能力コンポーネントの読み取り専用参照を取得します。</summary>
    public ref readonly JumpCapability Jump => ref Entity.Get<JumpCapability>();

    /// <summary>残り二段ジャンプ回数を取得します（HUD および外部クエリ用読み取り専用プロパティ）。</summary>
    public int RemainingJumps => Jump.RemainingJumps;

    /// <summary>
    ///     基底プレイヤーコンポーネントを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライトの画像ハンドル。</param>
    /// <param name="characterType">プレイヤーキャラクター種別。</param>
    protected PlayerPawn(Entity entity, PlayerConfig pCfg, int spriteHandle, CharacterType characterType) : base(entity)
    {
        Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");

        CharacterType = characterType;

        Entity.Set(new Transform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY))
           .Set(new Animation(characterType, "Idle"))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 32, 32)));
    }

    /// <summary>
    ///     自機 Pawn を指定設定で初期位置へリセットします。
    /// </summary>
    /// <param name="pCfg">リセット用プレイヤー設定。</param>
    public virtual void Reset(PlayerConfig pCfg)
    {
        Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");

        SetTransform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY);
        SetVelocity(0.0f, 0.0f);
        ResetHealth(pCfg.MaxHp);

        ref var c = ref Entity.GetMut<CollisionBox>();
        c.IsGrounded = false;

        Entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        Entity.Set(new SpringScale(1.0f, 1.0f, pCfg.SpringFrequency, pCfg.SpringDampingRatio));
        Entity.Set(new DashState());
        Entity.GetMut<MovementIntent>().Reset();
        Entity.GetMut<WeaponIntent>().Reset();

        PlayAnimation("Idle");
        SetFlipX(false);
        SetVisible(true);
        ClearTransientEffects();
        Entity.Set(new TimedEffectsState());
        Entity.Set(new GameplayTagState());
    }
}
