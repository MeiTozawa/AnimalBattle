using System.Diagnostics.CodeAnalysis;

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
    public float CollisionWidth { get; set; }
    public float CollisionHeight { get; set; }
    public string HurtboxShape { get; set; }
    public float HurtboxRadius { get; set; }
    public float HurtboxOffsetY { get; set; }
    public float ScaleX { get; set; }
    public float ScaleY { get; set; }
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
    public float ScaleX { get; set; }
    public float ScaleY { get; set; }

    // 被弾判定ボックス (Hurtbox)
    public string HurtboxShape { get; set; }
    public float HurtboxRadius { get; set; }
    public float HurtboxWidth { get; set; }
    public float HurtboxHeight { get; set; }
    public float HurtboxOffsetY { get; set; }

    // 接触ダメージ判定ボックス (Hitbox)
    public string HitboxShape { get; set; }
    public float BodyHitboxRadius { get; set; }
    public float BodyHitboxWidth { get; set; }
    public float BodyHitboxHeight { get; set; }
    public float BodyHitboxOffsetY { get; set; }
    public float BodyContactDamage { get; set; }

    // 地形衝突ボックス
    public float CollisionWidth { get; set; }
    public float CollisionHeight { get; set; }
}
