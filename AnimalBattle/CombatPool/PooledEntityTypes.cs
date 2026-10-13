namespace AnimalBattle.CombatPool;

/// <summary>
///     プール管理対象エンティティの所属種別を表す列挙体。
/// </summary>
public enum PooledEntityType : byte {
    /// <summary>自機通常弾。</summary>
    PlayerBullet = 0,

    /// <summary>Boss 弾幕。</summary>
    BossBullet = 1,

    /// <summary>スパーク・微小爆発などの点粒子エフェクト。</summary>
    SparkEffect = 2,

    /// <summary>ジャンプリングや衝撃波などの円環エフェクト。</summary>
    RingEffect = 3,

    /// <summary>近接斬撃や判定を伴うエリアエフェクト。</summary>
    AreaEffect = 4
}
