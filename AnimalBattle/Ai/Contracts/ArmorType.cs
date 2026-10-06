namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     技能実行中における耐性・スーパーアーマー種別を定義します。
/// </summary>
public enum ArmorType {
    /// <summary>通常状態（プレイヤーの攻撃で怯み・中断可能）。</summary>
    None = 0,

    /// <summary>スーパーアーマー（一定の靭性ダメージを超えるまで怯まない）。</summary>
    SuperArmor = 1,

    /// <summary>完全無敵（あらゆる被弾リアクションおよびダメージを無効化）。</summary>
    Invincible = 2
}
