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

    [Fact]
    public void Player_Update_ExecutesKinematicMovementAndWeapon_ConsumingIntents()
    {
        using var world = World.Create();
        world.Import<BulletModule>();

        var pEnt = world.Entity("P2");
        var player = new Player(pEnt, new PlayerConfig { MaxHp = 3, MaxJumps = 2, MoveSpeed = 200.0f, BulletSpeed = 400.0f }, -1);
        var controller = new PlayerController(player);
        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        controller.SubmitInput(moveX: 1.0f, jumpPressed: false, jumpHolding: false, shootPressed: true);
        player.Update(0.1f, new ArenaConfig { FloorLimitY = 600.0f, BoundaryMinX = 0.0f, BoundaryMaxX = 1000.0f }, platformQuery, world);
        world.Progress(0.1f);

        Assert.True(player.X > 0.0f);
        var bulletFound = false;
        world.QueryBuilder<Transform, Velocity>().With<PlayerBulletTag>().Build().Each((ref Transform _, ref Velocity _) => bulletFound = true);
        Assert.True(bulletFound);
    }

    [Fact]
    public void EndToEndPipeline_DrivesInput_Simulation_Damage_And_Animation()
    {
        using var world = World.Create();
        world.Import<BulletModule>();

        var platformQuery = world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        world.Entity("Ground")
            .Set(new Transform(100.0f, 132.0f, 1.0f, 1.0f))
            .Set(new CollisionBox(500.0f, 32.0f))
            .Add<PlatformTag>();

        // 1. Player と Boss1Controller を初期化
        var pEnt = world.Entity("P1");
        var player = new Player(pEnt, new PlayerConfig
        {
            SpawnX = 100.0f,
            SpawnY = 100.0f,
            MaxHp = 3,
            MaxJumps = 2,
            MoveSpeed = 200.0f,
            HitboxWidth = 32.0f,
            HitboxHeight = 32.0f,
            BulletSpeed = 400.0f,
            InvincibleDuration = 1.0f,
            HitFlashDuration = 0.08f
        }, -1);
        var pController = new PlayerController(player);

        var bEnt = world.Entity("B1");
        using IBossController boss = new Boss1Controller(bEnt, -1);

        // 2. コントローラー意思決定フェーズ（意図注入）
        pController.SubmitInput(moveX: 1.0f, jumpPressed: false, jumpHolding: false, shootPressed: true);
        boss.Update(0.016f, new Vec2f(player.X, player.Y), new Vec2f(player.Vx, player.Vy), world, platformQuery);

        // 3. ECS 全量シミュレーションパイプライン（Progress による一括推進）
        world.Progress(0.016f);
        world.Progress(0.016f);

        // 移動および射撃の自動実行を検証
        Assert.True(player.X > 100.0f);
        Assert.Equal("Run", player.Entity.Get<Animation>().CurrentClip);

        // 4. 被弾ダメージパイプラインの検証（敵攻撃判定をプレイヤーへ重ねて Progress）
        world.Entity("TestEnemyHitbox")
            .Set(new Transform(player.X, player.Y, 1.0f, 1.0f))
            .Set(new Hitbox(new CollisionShape.Circle(32.0f), 1.0f, Faction.Hostile, false));

        world.Progress(0.016f);

        // 衝突判定 -> ダメージ裁定 -> HP 減少、無敵時間、被弾フラッシュの自動付与を検証
        ICharacter playerChar = player;
        Assert.Equal(2.0f, playerChar.CurrentHp);
        Assert.True(playerChar.IsInvincible);
        Assert.True(playerChar.IsFlashing);
    }
}
