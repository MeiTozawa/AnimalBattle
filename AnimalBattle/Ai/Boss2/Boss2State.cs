namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2（Ground Crusher）の固有の行動状態を表す純粋な列挙体です。
/// </summary>
public enum Boss2State : byte
{
    /// <summary>安全な未指定状態です。</summary>
    None = 0,

    /// <summary>待機状態です。</summary>
    Idle = 1,

    /// <summary>足場追跡状態です。</summary>
    PatrolChase = 2,

    /// <summary>跳躍開始位置への移動状態です。</summary>
    MoveToTakeoff = 3,

    /// <summary>足場間機動（跳躍・落下・着地）状態です。</summary>
    PlatformTraverse = 4,

    /// <summary>攻撃予兆（前揺れ）状態です（全技能共通）。</summary>
    Telegraph = 5,

    /// <summary>攻撃判定実行状態です（全技能共通）。</summary>
    Execute = 6,

    /// <summary>壁激突気絶状態です。</summary>
    WallCrashStun = 7,

    /// <summary>攻撃後隙状態です（全技能共通）。</summary>
    Recovery = 8,

    /// <summary>Phase 遷移状態です。</summary>
    PhaseTransition = 9,

    /// <summary>死亡状態です。</summary>
    Dead = 10
}
