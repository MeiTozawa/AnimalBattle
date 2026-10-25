using System.Diagnostics.CodeAnalysis;
using AnimalBattle.Core;

#nullable disable

namespace AnimalBattle.Configs;

/// <summary>
///     戦闘関連の全 ECS プレハブ：Prefabの静的パラメータ設定。
///     TOML よりデシリアライズされます。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class CombatPrefabConfig
{
    /// <summary>戦闘判定・物理衝撃および重撃判定のグローバル設定。</summary>
    public CombatPhysicsConfig Physics { get; set; }

    /// <summary>自機通常弾のプレハブ設定。</summary>
    public PlayerBulletPrefabConfig PlayerBullet { get; set; }

    /// <summary>Boss 通常弾のプレハブ設定。</summary>
    public BossNormalBulletPrefabConfig BossNormalBullet { get; set; }

    /// <summary>Boss 品紅特殊弾のプレハブ設定。</summary>
    public BossMagentaBulletPrefabConfig BossMagentaBullet { get; set; }

    /// <summary>Boss 黄特殊弾のプレハブ設定。</summary>
    public BossYellowBulletPrefabConfig BossYellowBullet { get; set; }

    /// <summary>Boss 黄特殊弾の衛星弾プレハブ設定。</summary>
    public BossYellowSatellitePrefabConfig BossYellowSatellite { get; set; }

    /// <summary>Boss 青特殊弾のプレハブ設定。</summary>
    public BossBlueBulletPrefabConfig BossBlueBullet { get; set; }

    /// <summary>Boss 黒特殊弾のプレハブ設定。</summary>
    public BossBlackBulletPrefabConfig BossBlackBullet { get; set; }

    /// <summary>自機近接斬撃判定のプレハブ設定。</summary>
    public PlayerSlashHitboxPrefabConfig PlayerSlashHitbox { get; set; }

    /// <summary>Boss 2 近接斬撃判定のプレハブ設定。</summary>
    public BossSlashHitboxPrefabConfig BossSlashHitbox { get; set; }

    /// <summary>点粒子スパーク演出のプレハブ設定。</summary>
    public SparkVfxPrefabConfig SparkVfx { get; set; }

    /// <summary>リング拡散演出のプレハブ設定。</summary>
    public RingVfxPrefabConfig RingVfx { get; set; }

    /// <summary>判定付与エリア演出のプレハブ設定。</summary>
    public AreaVfxPrefabConfig AreaVfx { get; set; }
}

/// <summary>
///     グローバル戦闘物理および重撃閾値設定。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class CombatPhysicsConfig
{
    /// <summary>被弾時デフォルトノックバック初速度。</summary>
    public Vec2f DefaultKnockback { get; set; }

    /// <summary>重撃判定（強制ヒットストップ／カメラインパルス誘発）ダメージ閾値。</summary>
    public float HeavyDamageThreshold { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class PlayerBulletPrefabConfig
{
    public string Shape { get; set; }
    public Vec2f Size { get; set; }
    public float Damage { get; set; }
    public Vec2f Knockback { get; set; }
    public bool IsHeavy { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossNormalBulletPrefabConfig
{
    public float Radius { get; set; }
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossMagentaBulletPrefabConfig
{
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossYellowBulletPrefabConfig
{
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossYellowSatellitePrefabConfig
{
    public float MinRadius { get; set; }
    public float RadiusMultiplier { get; set; }
    public float OrbitDistanceMultiplier { get; set; }
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossBlueBulletPrefabConfig
{
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossBlackBulletPrefabConfig
{
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class PlayerSlashHitboxPrefabConfig
{
    public float Damage { get; set; }
    public Vec2f Size { get; set; }
    public float DefaultDuration { get; set; }
    public Vec2f Knockback { get; set; }
    public bool IsHeavy { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class SparkVfxPrefabConfig
{
    public float DefaultRadius { get; set; }
    public float DefaultDuration { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class RingVfxPrefabConfig
{
    public float InitialRadius { get; set; }
    public float MaxRadius { get; set; }
    public float DefaultDuration { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class AreaVfxPrefabConfig
{
    public float DefaultDuration { get; set; }
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossSlashHitboxPrefabConfig
{
    public float Damage { get; set; }
    public float DefaultDuration { get; set; }
    public Vec2f Knockback { get; set; }
    public bool IsHeavy { get; set; }
}
