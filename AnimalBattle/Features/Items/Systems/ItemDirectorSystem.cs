namespace AnimalBattle.Features.Items.Systems;

using System;
using AnimalBattle.Common.Components;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Player.Components;
using Components;
using Configs;
using Spawners;
using Flecs.NET.Core;

/// <summary>
///     アイテムディレクターのイベント購読およびプレイヤー体力参照クエリを保持するコンテキスト。
/// </summary>
internal sealed class ItemDirectorContext : IDisposable
{
    private readonly World _world;
    private IDisposable? _enemyDamagedSub;

    public Query<Health> PlayerHealthQuery { get; }

    public ItemDirectorContext(World world)
    {
        _world = world;
        PlayerHealthQuery = world.QueryBuilder<Health>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();
        _enemyDamagedSub = EventBus.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
    }

    private void OnEnemyDamaged(EnemyDamagedEvent evt)
    {
        ItemDirectorSystem.TryDropHealthPack(_world, this, evt);
    }

    public void Dispose()
    {
        _enemyDamagedSub?.Dispose();
        _enemyDamagedSub = null;
        PlayerHealthQuery.Dispose();
    }
}

/// <summary>
///     アイテムディレクターのクールダウンおよびタイマーを毎フレーム更新し、被弾イベントに基づくピンチ救済ドロップを判定するシステム。
/// </summary>
public static class ItemDirectorSystem
{
    /// <summary>
    ///     アイテムディレクター管理システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddItemDirectorSystem(this World world)
    {
        world.SetDisposable(new ItemDirectorContext(world));

        world.System<ItemDirectorState>("ItemDirectorSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(static (Iter it, Field<ItemDirectorState> directors) =>
            {
                var count = it.Count();
                if (count == 0) return;

                var dt = it.DeltaTime();
                if (dt <= 0.0f) return;

                for (var i = 0; i < count; i++)
                {
                    ref var director = ref directors[i];
                    if (director.IcdRemaining > 0.0f)
                    {
                        director.IcdRemaining = MathF.Max(0.0f, director.IcdRemaining - dt);
                    }
                }
            });

        return world;
    }

    /// <summary>
    ///     敵ユニット（ボス・雑魚敵・ダミー）の被弾または撃破時に、ピンチ救済アルゴリズムに基づいて回復アイテムのドロップ判定を実行します。
    /// </summary>
    internal static void TryDropHealthPack(World world, ItemDirectorContext ctx, EnemyDamagedEvent evt)
    {
        var itemCfg = world.Get<ItemConfig>();
        var cfg = itemCfg.HealthPack;
        ref var director = ref world.GetMut<ItemDirectorState>();

        if (director.ActiveHealthPackCount >= cfg.MaxActiveCount || director.IcdRemaining > 0f) return;

        ctx.PlayerHealthQuery.Each((ref Health playerHp) =>
        {
            // プレイヤーのHPが最大値の場合はドロップしない（リソース温存）
            if (playerHp.Current >= playerHp.Max) return;

            var playerRatio = playerHp.Max > 0f ? Math.Clamp(playerHp.Current / playerHp.Max, 0f, 1f) : 1f;
            var targetRatio = evt.MaxHp > 0f ? Math.Clamp(evt.CurrentHp / evt.MaxHp, 0f, 1f) : 1f;

            var baseRate = cfg.BaseDropRate 
                         + (1.0f - playerRatio) * cfg.PlayerWeight 
                         + (1.0f - targetRatio) * cfg.BossWeight;
            var finalRate = Math.Clamp(baseRate * (1.0f + evt.FinalDamage * cfg.DamageScale), 0.0f, cfg.MaxDropRate);

            if (Random.Shared.NextSingle() < finalRate)
            {
                ItemSpawner.SpawnHealthPack(world, evt.Position, cfg);
            }
        });
    }
}
