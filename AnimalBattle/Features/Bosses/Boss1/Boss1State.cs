namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss1：Ancient Cat Overlordの固有の行動状態を表す純粋な列挙体です。
/// </summary>
public enum Boss1State : byte {
    /// <summary>安全な未指定状態です。</summary>
    None = 0,

    /// <summary>浮遊移動状態です。</summary>
    Hover = 1,

    /// <summary>攻撃選択状態です。</summary>
    AttackSelection = 2,

    /// <summary>攻撃予兆：前揺れ状態です：全技能共通。</summary>
    Telegraph = 3,

    /// <summary>攻撃判定実行：弾幕・突進状態です：全技能共通。</summary>
    Execute = 4,

    /// <summary>攻撃後隙状態です：全技能共通。</summary>
    Recovery = 5,

    /// <summary>Phase 遷移状態です。</summary>
    PhaseTransition = 6,

    /// <summary>死亡状態です。</summary>
    Dead = 7
}