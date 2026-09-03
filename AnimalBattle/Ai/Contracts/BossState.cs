namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 実行時に使用する固定 Boss AI 状態を表します。
/// </summary>
/// <remarks>
/// 値の追加・削除・実行時登録は行わず、各値を具体的な State_Module に静的に対応させます。
/// </remarks>
public enum BossState
{
    /// <summary>安全な未指定状態です。</summary>
    Unknown = 0,

    /// <summary>Boss1 の浮遊移動状態です。</summary>
    Boss1_Hover,
    /// <summary>Boss1 の攻撃選択状態です。</summary>
    Boss1_AttackSelection,
    /// <summary>Boss1 の攻撃予兆（前揺れ）状態です（全技能共通）。</summary>
    Boss1_Telegraph,
    /// <summary>Boss1 の攻撃判定実行（弾幕・突進）状態です（全技能共通）。</summary>
    Boss1_Execute,
    /// <summary>Boss1 の攻撃後隙状態です（全技能共通）。</summary>
    Boss1_Recovery,
    /// <summary>Boss1 の Phase 遷移状態です。</summary>
    Boss1_PhaseTransition,
    /// <summary>Boss1 の Dead 状態です。</summary>
    Boss1_Dead,

    /// <summary>Boss2 の待機状態です。</summary>
    Boss2_Idle,
    /// <summary>Boss2 の足場追跡状態です。</summary>
    Boss2_PatrolChase,
    /// <summary>Boss2 の跳躍開始位置への移動状態です。</summary>
    Boss2_MoveToTakeoff,
    /// <summary>Boss2 の足場間機動（跳躍・落下・着地）状態です。</summary>
    Boss2_PlatformTraverse,
    /// <summary>Boss2 の攻撃予兆（前揺れ）状態です（全技能共通）。</summary>
    Boss2_Telegraph,
    /// <summary>Boss2 の攻撃判定実行状態です（全技能共通）。</summary>
    Boss2_Execute,
    /// <summary>Boss2 の壁激突気絶状態です。</summary>
    Boss2_WallCrashStun,
    /// <summary>Boss2 の攻撃後隙状態です（全技能共通）。</summary>
    Boss2_Recovery,
    /// <summary>Boss2 の Phase 遷移状態です。</summary>
    Boss2_PhaseTransition,
    /// <summary>Boss2 の Dead 状態です。</summary>
    Boss2_Dead
}

/// <summary>固定 BossState の判定を提供します。</summary>
public static class BossStateExtensions
{
    /// <summary>Unknown ではない固定状態かどうかを取得します。</summary>
    public static bool IsValid(this BossState state) => state != BossState.Unknown;
}
