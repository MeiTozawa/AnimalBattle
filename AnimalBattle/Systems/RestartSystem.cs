using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Input;
using AnimalBattle.Scenes;

namespace AnimalBattle.Systems;

/// <summary>
/// R キー押下による戦場・プレイヤー・Boss の即時リセット、
/// および 1 / 2 キー押下による Boss 1 (Ancient Cat) / Boss 2 (Ground Crusher) 即時切り替えを司る ECS System。
/// </summary>
public static class RestartSystem
{
    /// <summary>
    /// リスタートおよび Boss 切替システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddRestartSystem(this World world)
    {
        Query playerBulletsQuery =
            world.QueryBuilder()
                 .With<PlayerBulletTag>()
                 .Build();

        Query effectsQuery =
            world.QueryBuilder()
                 .With<EffectTag>()
                 .Build();

        Query<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite> playerQuery =
            world.QueryBuilder<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite>()
                 .With<PlayerTag>()
                 .Build();

        Query<BossCompatibilityData, BossTerminalData> bossRestartQuery =
            world.QueryBuilder<BossCompatibilityData, BossTerminalData>()
                 .With<BossTag>()
                 .Build();

        world.System("RestartSystem")
             .Kind(Ecs.PostUpdate)
             .Run(it =>
             {
                 bool restartJustPressed = InputService.IsPressed(InputAction.Restart);
                 bool selectBoss1Pressed = InputService.IsPressed(InputAction.SelectBoss1);
                 bool selectBoss2Pressed = InputService.IsPressed(InputAction.SelectBoss2);

                 if (selectBoss1Pressed)
                 {
                     BattleScene.Current?.SwitchBoss("Boss1");
                     ResetPlayer(world, playerBulletsQuery, effectsQuery, playerQuery);
                 }
                 else if (selectBoss2Pressed)
                 {
                     BattleScene.Current?.SwitchBoss("Boss2");
                     ResetPlayer(world, playerBulletsQuery, effectsQuery, playerQuery);
                 }
                 else if (restartJustPressed)
                 {
                     RestartBossFromCompatibilityView(bossRestartQuery);
                     ResetPlayer(world, playerBulletsQuery, effectsQuery, playerQuery);
                 }
             });

        return world;
    }

    private static void RestartBossFromCompatibilityView(Query<BossCompatibilityData, BossTerminalData> bossRestartQuery)
    {
        bool restarted = false;
        bossRestartQuery.Each((ref BossCompatibilityData compatibility, ref BossTerminalData terminal) =>
        {
            if (restarted)
            {
                return;
            }

            BattleScene.Current?.ResetCurrentBoss(in compatibility, in terminal);
            restarted = true;
        });

        if (!restarted)
        {
            BattleScene.Current?.ResetCurrentBoss();
        }
    }

    private static void ResetPlayer(World world, Query playerBulletsQuery, Query effectsQuery, Query<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite> playerQuery)
    {
        PlayerConfig pCfg = ConfigManager.Player;

        CombatEffectHelper.ClearAllBossBullets(world);
        playerBulletsQuery.Each(e => e.Destruct());
        effectsQuery.Each(e => e.Destruct());

        playerQuery.Each((Entity e, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health health, ref DoubleJump doubleJump, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
        {
            trans.X = pCfg.SpawnX;
            trans.Y = pCfg.SpawnY;
            trans.ScaleX = pCfg.ScaleX;
            trans.ScaleY = pCfg.ScaleY;
            vel.Vx = 0.0f;
            vel.Vy = 0.0f;
            health.Current = pCfg.MaxHp;
            health.Max = pCfg.MaxHp;
            health.InvincibleTimer = 0.0f;
            doubleJump.RemainingJumps = doubleJump.MaxJumps;
            playerState.ShootCooldown = 0.0f;
            col.Width = pCfg.HitboxWidth;
            col.Height = pCfg.HitboxHeight;
            col.IsGrounded = true;
            anim.CurrentClip = "Idle";
            anim.Timer = 0.0f;
            sprite.IsVisible = true;
            sprite.FlipX = false;

            e.Set(new Hurtbox(pCfg.GetHurtboxShape(), Faction.Friendly));
        });
    }
}
