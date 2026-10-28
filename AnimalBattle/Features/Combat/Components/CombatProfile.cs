using AnimalBattle.Ai;

namespace AnimalBattle.Combat.Components;

/// <summary>
///     戦闘エンティティのロール種別を定義します。
/// </summary>
public enum CombatEntityKind : byte
{
    /// <summary>自機プレイヤー。</summary>
    Player = 0,

    /// <summary>一般敵・小怪（Minion）。</summary>
    Enemy = 1,

    /// <summary>ステージBoss。</summary>
    Boss = 2,

    /// <summary>手触り検証用木桩（Training Dummy）。</summary>
    Dummy = 3
}

/// <summary>
///     全戦闘ユニット（プレイヤー、Boss、小怪、木桩）の被撃・反撃・死亡ポリシーを一元管理する統一データコンポーネント。
/// </summary>
public struct CombatProfile
{
    /// <summary>エンティティの種別ロール。</summary>
    public CombatEntityKind Kind;

    /// <summary>ノックバック外力を受けるか。</summary>
    public bool CanBeKnockedBack;

    /// <summary>被撃時に反撃弾幕を展開するか。</summary>
    public bool HasCounterAttack;

    /// <summary>現在の戦闘フェーズ（Bossの反撃弾幕数決定等に使用）。</summary>
    public Phase Phase;

    /// <summary>被弾後の無敵時間秒数（自機用）。</summary>
    public float InvincibleDuration;

    /// <summary>プレイヤー用の戦闘プロファイルを生成します。</summary>
    public static CombatProfile ForPlayer(float invDuration) => new()
    {
        Kind = CombatEntityKind.Player,
        CanBeKnockedBack = true,
        HasCounterAttack = false,
        Phase = Phase.Phase1,
        InvincibleDuration = invDuration
    };

    /// <summary>Boss用の戦闘プロファイルを生成します。</summary>
    public static CombatProfile ForBoss(bool knockback) => new()
    {
        Kind = CombatEntityKind.Boss,
        CanBeKnockedBack = knockback,
        HasCounterAttack = true,
        Phase = Phase.Phase1,
        InvincibleDuration = 0f
    };

    /// <summary>一般敵・小怪用の戦闘プロファイルを生成します。</summary>
    public static CombatProfile ForMinion() => new()
    {
        Kind = CombatEntityKind.Enemy,
        CanBeKnockedBack = true,
        HasCounterAttack = false,
        Phase = Phase.Phase1,
        InvincibleDuration = 0f
    };

    /// <summary>手触り検証用木桩の戦闘プロファイルを生成します。</summary>
    public static CombatProfile ForDummy() => new()
    {
        Kind = CombatEntityKind.Dummy,
        CanBeKnockedBack = true,
        HasCounterAttack = false,
        Phase = Phase.Phase1,
        InvincibleDuration = 0f
    };
}
