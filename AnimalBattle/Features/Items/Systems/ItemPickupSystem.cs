namespace AnimalBattle.Features.Items.Systems;

using System;
using AnimalBattle.Combat.Components;
using AnimalBattle.Common.Components;
using Core;
using Events;
using Components;
using AnimalBattle.Player.Components;
using Flecs.NET.Core;

/// <summary>
///     アイテム回収システム用キャッシュクエリを保持するコンテキストリソース。
/// </summary>
internal sealed class ItemPickupContext : IDisposable
{
    /// <summary>生存中のプレイヤー抽出クエリ。</summary>
    public required Query<Transform, Health> PlayerQuery { get; init; }

    public void Dispose()
    {
        PlayerQuery.Dispose();
    }
}

/// <summary>
///     プレイヤーと回復アイテムの接触を判定し、体力回復と効果音再生・破棄を処理するシステム。
/// </summary>
public static class ItemPickupSystem
{
    /// <summary>
    ///     アイテム回収判定システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddItemPickupSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<Transform, Health>()
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Build();

        world.Set(new ItemPickupContext
        {
            PlayerQuery = playerQuery
        });

        world.System<Transform, ItemPickup>("ItemPickupSystem")
            .With<HealthPackTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(Execute);

        return world;
    }

    private static void Execute(Iter it, Field<Transform> transforms, Field<ItemPickup> pickups)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        if (world.Has<GameOverTag>()) return;

        var ctx = world.Get<ItemPickupContext>();
        Entity? targetPlayer = null;
        Vec2f playerPos = Vec2f.Zero;
        Health playerHp = default;

        ctx.PlayerQuery.Each((Entity e, ref Transform tr, ref Health hp) =>
        {
            if (hp.Current > 0.0f)
            {
                targetPlayer = e;
                playerPos = tr.Position;
                playerHp = hp;
            }
        });

        if (targetPlayer == null) return;

        // プレイヤーのHPが最大値の場合は取得を行わない（温存）
        if (playerHp.Current >= playerHp.Max) return;

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref readonly var tr = ref transforms[i];
            ref readonly var pickup = ref pickups[i];

            var diff = playerPos - tr.Position;
            var distSq = diff.LengthSquared();
            var reach = pickup.Radius;

            if (distSq <= reach * reach)
            {
                // 回復処理の適用
                ref var hpMut = ref targetPlayer.Value.GetMut<Health>();
                var prevHp = hpMut.Current;
                hpMut.Current = MathF.Min(hpMut.Max, hpMut.Current + pickup.HealAmount);

                // HUD 更新イベント通知
                EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hpMut.Current));

                // 回復効果音再生
                SoundService.PlaySfx(SfxType.ItemHeal);

                // ディレクター状態の更新とエンティティ消滅
                ref var director = ref world.GetMut<ItemDirectorState>();
                director.ActiveHealthPackCount = Math.Max(0, director.ActiveHealthPackCount - 1);
                entity.Destruct();
                break;
            }
        }
    }
}
