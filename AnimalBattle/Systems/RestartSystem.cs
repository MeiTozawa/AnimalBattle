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
                     ResetPlayer(world, playerBulletsQuery, effectsQuery);
                 }
                 else if (selectBoss2Pressed)
                 {
                     BattleScene.Current?.SwitchBoss("Boss2");
                     ResetPlayer(world, playerBulletsQuery, effectsQuery);
                 }
                 else if (restartJustPressed)
                 {
                     BattleScene.Current?.ResetCurrentBoss();
                     ResetPlayer(world, playerBulletsQuery, effectsQuery);
                 }
             });

        return world;
    }

    private static void ResetPlayer(World world, Query playerBulletsQuery, Query effectsQuery)
    {
        PlayerConfig pCfg = ConfigManager.Player;

        CombatEffectHelper.ClearAllBossBullets(world);
        playerBulletsQuery.Each(e => e.Destruct());
        effectsQuery.Each(e => e.Destruct());

        BattleScene.Current?.Player?.Reset(pCfg);
    }
}
