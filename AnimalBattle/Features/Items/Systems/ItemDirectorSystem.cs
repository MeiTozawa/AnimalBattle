namespace AnimalBattle.Features.Items.Systems;

using System;
using AnimalBattle.Features.Items.Components;
using Flecs.NET.Core;

/// <summary>
///     アイテムディレクターのクールダウンおよびタイマーを毎フレーム更新するシステム。
/// </summary>
public static class ItemDirectorSystem
{
    /// <summary>
    ///     アイテムディレクター管理システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddItemDirectorSystem(this World world)
    {
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
}
