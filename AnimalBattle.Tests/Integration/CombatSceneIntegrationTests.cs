using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Modules;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Integration;

/// <summary>
///     戦闘シーン端到端パイプライン（Controller -> Physics -> Weapon -> ECS）の統合検証テスト。
/// </summary>
public class CombatSceneIntegrationTests
{
    [Fact]
    public void SceneFramePipeline_ExecutesControllerInput_ThenPhysics_ThenWeapon()
    {
        using var world = World.Create();
        world.Import<BulletModule>();

        // 1. Player Pawn と Controller の初期化
        var pEnt = world.Entity("P");
        var player = new Player(pEnt, new PlayerConfig { MaxHp = 3, MaxJumps = 2, MoveSpeed = 200.0f, BulletSpeed = 400.0f }, -1);
        var controller = new PlayerController(player);

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        // 2. 右移動および射撃意図の注入
        controller.SubmitInput(moveX: 1.0f, jumpPressed: false, jumpHolding: false, shootPressed: true);

        // 3. パイプラインの実行
        KinematicMovementSystem.UpdateEntity(pEnt, 0.1f, platformQuery);
        CharacterWeaponSystem.UpdateEntity(pEnt, 0.1f, world);

        // 4. 座標前進および弾丸生成の検証
        Assert.True(player.X > 0.0f);
        var bulletFound = false;
        world.QueryBuilder<Transform, Velocity>().With<PlayerBulletTag>().Build().Each((ref Transform _, ref Velocity _) => bulletFound = true);
        Assert.True(bulletFound);
    }
}
