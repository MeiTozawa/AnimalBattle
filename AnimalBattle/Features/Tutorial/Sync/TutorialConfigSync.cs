using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Spawners;
using AnimalBattle.Logging;

namespace AnimalBattle.Features.Tutorial.Sync;

/// <summary>
///     チュートリアル設定（tutorial.toml）ホットリロード時に、Flecs.NET ワールドの単例および活体フェーズエンティティを動的に同期・再水和する機構。
/// </summary>
public static class TutorialConfigSync
{
    /// <summary>
    ///     チュートリアル設定の変更をワールドおよび実行中フェーズへ即座に反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の TutorialConfig 設定</param>
    public static void Sync(World world, TutorialConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "TutorialConfig は null にできません。");
        if (newConfig == null) return;

        // 1. ワールド単例コンポーネントの更新
        world.Set(newConfig);

        // 2. チュートリアル実行中の場合、現在フェーズのエンティティ（目標物／小怪／扉）を最新設定で即座に再生成
        if (world.Has<GameFlow>() && world.Get<GameFlow>().State == GameState.Tutorial)
        {
            if (world.Has<TutorialState>())
            {
                ref var state = ref world.GetMut<TutorialState>();

                // フェーズ配列数が削減された場合の安全クランプ
                if (newConfig.Phases.Length > 0 && state.CurrentPhaseIndex >= newConfig.Phases.Length)
                {
                    state.CurrentPhaseIndex = Math.Max(0, newConfig.Phases.Length - 1);
                }

                TutorialSceneSpawner.ResetCurrentPhase(world);
                Log.Info("TutorialConfigSync", $"[チュートリアル設定] tutorial.toml のホットリロードを適用しました (PhaseIndex: {state.CurrentPhaseIndex})");
            }
        }
    }
}
