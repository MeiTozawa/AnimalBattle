using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

/// <summary>
///     汎用武器射撃システム <see cref="WeaponSystem"/> の冷却および弾丸生成テスト。
/// </summary>
public class CharacterWeaponSystemTests
{
    [Fact]
    public void WeaponSystem_TriggerShoot_WhenCooldownZero_GeneratesBullet_And_SetsCooldown()
    {
        using var world = World.Create();

        var entity = world.Entity("Shooter")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new WeaponIntent { ShootTriggered = true, AimDirectionX = 1.0f, AimDirectionY = 0.0f })
            .Set(new WeaponState
            {
                CooldownRemaining = 0.0f,
                CooldownDuration = 0.2f,
                BulletSpeed = 400.0f,
                BulletDamage = 1.0f,
                BulletWidth = 8.0f,
                BulletHeight = 8.0f,
                BulletSpawnOffset = 24.0f,
                BulletSpawnOffsetY = 0.0f
            });

        WeaponSystem.UpdateEntity(entity, 0.016f, world);

        ref readonly var weapon = ref entity.Get<WeaponState>();
        Assert.Equal(0.2f, weapon.CooldownRemaining);

        // 世界内に自機弾丸エンティティが生成されたことを検証
        var bulletCount = 0;
        world.QueryBuilder<Transform, Velocity>().With<PlayerBulletTag>().Build().Each((ref Transform _, ref Velocity _) => bulletCount++);
        Assert.Equal(1, bulletCount);
    }

    [Fact]
    public void WeaponSystem_TriggerShoot_WhenCooldownActive_DoesNotGenerateBullet()
    {
        using var world = World.Create();

        var entity = world.Entity("Shooter")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new WeaponIntent { ShootTriggered = true, AimDirectionX = 1.0f, AimDirectionY = 0.0f })
            .Set(new WeaponState
            {
                CooldownRemaining = 0.15f,
                CooldownDuration = 0.2f,
                BulletSpeed = 400.0f,
                BulletDamage = 1.0f
            });

        WeaponSystem.UpdateEntity(entity, 0.05f, world);

        ref readonly var weapon = ref entity.Get<WeaponState>();
        Assert.True(weapon.CooldownRemaining > 0.0f);

        // 弾丸が生成されていないことを検証
        var bulletCount = 0;
        world.QueryBuilder<Transform, Velocity>().With<PlayerBulletTag>().Build().Each((ref Transform _, ref Velocity _) => bulletCount++);
        Assert.Equal(0, bulletCount);
    }

    [Fact]
    public void CharacterWeaponSystem_ExecutesInWorldProgress_Automatically()
    {
        using var world = World.Create();
        world.AddWeaponSystem();

        var entity = world.Entity("Shooter")
            .Set(new Transform(100.0f, 100.0f, 1.0f, 1.0f))
            .Set(new WeaponIntent { ShootTriggered = true, AimDirectionX = 1.0f, AimDirectionY = 0.0f })
            .Set(new WeaponState
            {
                CooldownRemaining = 0.0f,
                CooldownDuration = 0.2f,
                BulletSpeed = 400.0f,
                BulletDamage = 1.0f,
                BulletWidth = 8.0f,
                BulletHeight = 8.0f,
                BulletSpawnOffset = 24.0f,
                BulletSpawnOffsetY = 0.0f
            });

        world.Progress(0.016f);

        ref readonly var weapon = ref entity.Get<WeaponState>();
        Assert.Equal(0.2f, weapon.CooldownRemaining);

        // 世界内に自機弾丸エンティティが生成されたことを検証
        var bulletCount = 0;
        world.QueryBuilder<Transform, Velocity>().With<PlayerBulletTag>().Build().Each((ref Transform _, ref Velocity _) => bulletCount++);
        Assert.Equal(1, bulletCount);
    }
}
