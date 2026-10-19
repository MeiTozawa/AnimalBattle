using System.Diagnostics.CodeAnalysis;
using AnimalBattle.Core;

#nullable disable

namespace AnimalBattle.Configs;

/// <summary>
///     プレイヤーキャラクター（Cat / Bunny）の ECS 原型パラメータ設定。
///     TOML (cat.prefab.toml, bunny.prefab.toml) よりデシリアライズされます。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class PlayerPrefabConfig
{
    public float MaxHp { get; set; }
    public float InvincibleDuration { get; set; }
    public float HitFlashDuration { get; set; }
    public Vec2f CollisionSize { get; set; }
    public string HurtboxShape { get; set; }
    public float HurtboxRadius { get; set; }
    public Vec2f HurtboxOffset { get; set; }
    public Vec2f Scale { get; set; }
    public float SpringFrequency { get; set; }
    public float SpringDampingRatio { get; set; }
}

/// <summary>
///     Boss キャラクター（Boss1 / Boss2）の ECS 原型パラメータ設定。
///     TOML (boss1.prefab.toml, boss2.prefab.toml) よりデシリアライズされます。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class BossPrefabConfig
{
    public float MaxHp { get; set; }
    public float HitFlashDuration { get; set; }
    public Vec2f Scale { get; set; }

    // 被弾判定ボックス (Hurtbox)
    public string HurtboxShape { get; set; }
    public float HurtboxRadius { get; set; }
    public Vec2f HurtboxSize { get; set; }
    public Vec2f HurtboxOffset { get; set; }

    // 接触ダメージ判定ボックス (Hitbox)
    public string HitboxShape { get; set; }
    public float BodyHitboxRadius { get; set; }
    public Vec2f BodyHitboxSize { get; set; }
    public Vec2f BodyHitboxOffset { get; set; }
    public float BodyContactDamage { get; set; }

    // 地形衝突ボックス
    public Vec2f CollisionSize { get; set; }
}
