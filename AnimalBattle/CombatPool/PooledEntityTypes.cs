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

/// <summary>
///     プール管理対象エンティティであることを示す識別コンポーネント。
///     リサイクル時に対応するプールへの返却を O(1) で判定します。
/// </summary>
public readonly record struct PooledEntity(PooledEntityType PoolType);

/// <summary>
///     エンティティのプール貸出状態および所属プール代際を識別するコンポーネント。
/// </summary>
public struct PoolLeaseState {
    /// <summary>所属プールの固有識別子。</summary>
    public ulong PoolId;

    /// <summary>現在貸出中かどうか。</summary>
    public bool IsRented;
}
