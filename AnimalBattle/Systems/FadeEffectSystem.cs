using System;
using AnimalBattle.Components;
using AnimalBattle.Compute;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// イージング関数に基づく透明度フェードイン・フェードアウトの進行を制御し、持続時間満了時にエンティティを自動破棄する ECS System。
/// <see cref="NativeComputeQueue"/> を用いて全アクティブフェード効果のイージング評価を一括同期バッチ実行します。
/// </summary>
public static class FadeEffectSystem
{
    /// <summary>
    /// フェード演出および自動寿命破棄システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World"/>。</returns>
    public static World AddFadeEffectSystem(this World world)
    {
        world.System<FadeEffect>("FadeEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Iter((Iter it, Field<FadeEffect> fade) =>
             {
                 int count = it.Count();
                 if (count <= 0)
                 {
                     return;
                 }

                 float dt = it.DeltaTime();
                 NativeComputeQueue queue = NativeComputeQueue.Shared;
                 queue.Clear();

                 // 1. 各 FadeEffect の寿命判定と進行要求の登録
                 Span<int> handles = stackalloc int[count];
                 for (int i = 0; i < count; ++i)
                 {
                     fade[i].ElapsedTimer += dt;
                     if (fade[i].ElapsedTimer >= fade[i].Duration)
                     {
                         handles[i] = -1;
                         continue;
                     }

                     float progress = Math.Clamp(fade[i].ElapsedTimer / Math.Max(0.001f, fade[i].Duration), 0.0f, 1.0f);
                     handles[i] = queue.EnqueueEase(fade[i].Ease, progress);
                 }

                 // 2. 1 回の P/Invoke で C++ 原生拡張へ一括転送・並列 SIMD 実行
                 queue.Dispatch();

                 // 3. 結果の反映および寿命満了エンティティの破棄
                 for (int i = 0; i < count; ++i)
                 {
                     if (handles[i] < 0)
                     {
                         it.Entity(i).Destruct();
                     }
                     else
                     {
                         float factor = queue.GetEaseResult(handles[i]);
                         fade[i].CurrentAlpha = fade[i].StartAlpha + (fade[i].EndAlpha - fade[i].StartAlpha) * factor;
                     }
                 }

                 queue.Clear();
             });

        return world;
    }
}
