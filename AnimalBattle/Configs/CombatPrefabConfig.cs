using System.Diagnostics.CodeAnalysis;

namespace AnimalBattle.Configs;

/// <summary>
///     戦闘関連の全 ECS 原型（Prefab）の静的パラメータ設定。
///     TOML (combat.prefab.toml) よりデシリアライズされます。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class CombatPrefabConfig
{
    /// <summary>自機通常弾の原型設定。</summary>
    public PlayerBulletPrefabConfig PlayerBullet { get; set; } = new();

    /// <summary>Boss 通常弾の原型設定。</summary>
    public BossNormalBulletPrefabConfig BossNormalBullet { get; set; } = new();

    /// <summary>Boss 品紅特殊弾の原型設定。</summary>
    public BossMagentaBulletPrefabConfig BossMagentaBullet { get; set; } = new();

    /// <summary>Boss 黄特殊弾の原型設定。</summary>
    public BossYellowBulletPrefabConfig BossYellowBullet { get; set; } = new();

    /// <summary>Boss 黄特殊弾の衛星弾原型設定。</summary>
    public BossYellowSatellitePrefabConfig BossYellowSatellite { get; set; } = new();

    /// <summary>Boss 青特殊弾の原型設定。</summary>
    public BossBlueBulletPrefabConfig BossBlueBullet { get; set; } = new();

    /// <summary>Boss 黒特殊弾の原型設定。</summary>
    public BossBlackBulletPrefabConfig BossBlackBullet { get; set; } = new();

    /// <summary>自機近接斬撃判定の原型設定。</summary>
    public PlayerSlashHitboxPrefabConfig PlayerSlashHitbox { get; set; } = new();

    /// <summary>点粒子スパーク演出の原型設定。</summary>
    public SparkVfxPrefabConfig SparkVfx { get; set; } = new();

    /// <summary>リング拡散演出の原型設定。</summary>
    public RingVfxPrefabConfig RingVfx { get; set; } = new();

    /// <summary>判定付与エリア演出の原型設定。</summary>
    public AreaVfxPrefabConfig AreaVfx { get; set; } = new();
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class PlayerBulletPrefabConfig
{
    public string Shape { get; set; } = string.Empty;
    public float Width { get; set; }
    public float Height { get; set; }
    public float Damage { get; set; }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossNormalBulletPrefabConfig
{
    public float Radius { get; set; }
    public float Damage { get; set; }
    public string FillColor { get; set; } = string.Empty;
    public string OutlineColor { get; set; } = string.Empty;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossMagentaBulletPrefabConfig
{
    public float Radius { get; set; }
    public float Damage { get; set; }
    public string FillColor { get; set; } = string.Empty;
    public string OutlineColor { get; set; } = string.Empty;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossYellowBulletPrefabConfig
{
    public float Radius { get; set; }
    public float Damage { get; set; }
    public string FillColor { get; set; } = string.Empty;
    public string OutlineColor { get; set; } = string.Empty;
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
    public float Radius { get; set; }
    public float Damage { get; set; }
    public string FillColor { get; set; } = string.Empty;
    public string OutlineColor { get; set; } = string.Empty;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class BossBlackBulletPrefabConfig
{
    public float Radius { get; set; }
    public float Damage { get; set; }
    public string FillColor { get; set; } = string.Empty;
    public string OutlineColor { get; set; } = string.Empty;
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class PlayerSlashHitboxPrefabConfig
{
    public float Damage { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
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
