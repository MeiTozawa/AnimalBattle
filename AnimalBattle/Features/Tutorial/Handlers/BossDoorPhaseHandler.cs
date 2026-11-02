using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Handlers;

/// <summary>
///     ボス挑戦扉フェーズの生成および待機ハンドラー。
/// </summary>
public sealed class BossDoorPhaseHandler : ITutorialPhaseHandler
{
    /// <inheritdoc />
    public void Setup(World world, Entity root, TutorialPhaseConfig config, TutorialConfig globalConfig)
    {
        Debug.Assert(config != null, "フェーズ設定が null です。");
        if (config == null) return;

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining = 0;

        var interactionRadius = globalConfig.Doors.InteractionRadius;

        for (var i = 0; i < config.Doors.Length; i++)
        {
            var item = config.Doors[i];
            var parseSuccess = Enum.TryParse<BossType>(item.BossType, ignoreCase: true, out var bossType);
            Debug.Assert(parseSuccess, $"不正な BossType です: {item.BossType}");
            if (!parseSuccess) continue;

            world.Entity($"TutorialDoor_{bossType}_{i}")
                .ChildOf(root)
                .Set(new Transform(item.Pos, Vec2f.One))
                .Set(new TutorialDoor
                {
                    TargetBoss = bossType,
                    InteractionRadius = interactionRadius,
                    IsPlayerInRange = false,
                    AnimTimer = 0f
                });
        }
    }

    /// <inheritdoc />
    public bool UpdateAndCheckCompletion(World world, ref TutorialState state, TutorialPhaseConfig config, TutorialConfig globalConfig, float dt)
    {
        // ボス挑戦扉はプレイヤーの F キー押下インタラクションにより遷移するため、フロー側では自動完了しない
        return false;
    }
}
