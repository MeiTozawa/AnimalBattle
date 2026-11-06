using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Actions;
using AnimalBattle.Features.Tutorial.Conditions;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Factories;

/// <summary>
///     チュートリアル設定からアクションおよび条件のインスタンスを生成するファクトリクラス。
/// </summary>
public static class TutorialStepFactory
{
    /// <summary>
    ///     アクション設定から <see cref="ITutorialAction" /> を生成します。
    /// </summary>
    public static ITutorialAction CreateAction(TutorialActionConfig config)
    {
        Debug.Assert(config != null, "TutorialActionConfig が null です。");
        if (config == null) return null!;

        return config.Type.ToLowerInvariant() switch
        {
            "spawntarget" => new SpawnTargetAction(config.Pos, config.Radius),
            "spawnminion" => CreateSpawnMinionAction(config.MinionType),
            "spawndoor" => CreateSpawnDoorAction(config.Pos, config.BossType),
            "damageplayer" => new DamagePlayerAction(config.Amount > 0f ? config.Amount : 30f),
            "spawnhealthpack" => new SpawnHealthPackAction(config.Pos),
            _ => AssertHelper.FailWithDefault<ITutorialAction>($"未知のチュートリアルアクション種別です: {config.Type}", null!)
        };
    }

    /// <summary>
    ///     条件設定から <see cref="ITutorialCondition" /> を生成します。
    /// </summary>
    public static ITutorialCondition CreateCondition(TutorialConditionConfig config)
    {
        Debug.Assert(config != null, "TutorialConditionConfig が null です。");
        if (config == null) return null!;

        return config.Type.ToLowerInvariant() switch
        {
            "collecttargets" => new CollectTargetsCondition(),
            "collecthealthpacks" => new CollectHealthPacksCondition(),
            "defeatminions" => new DefeatMinionsCondition(),
            "interactdoor" => new InteractDoorCondition(),
            "timer" => new TimerCondition(config.Duration),
            _ => AssertHelper.FailWithDefault<ITutorialCondition>($"未知のチュートリアル条件種別です: {config.Type}", null!)
        };
    }

    private static ITutorialAction CreateSpawnMinionAction(string minionTypeStr)
    {
        var type = minionTypeStr.Equals("Flying", StringComparison.OrdinalIgnoreCase)
            ? MinionType.Flying
            : minionTypeStr.Equals("Ground", StringComparison.OrdinalIgnoreCase)
                ? MinionType.Ground
                : AssertHelper.FailWithDefault($"未知のミニオン種別です: {minionTypeStr}", MinionType.Ground);

        return new SpawnMinionAction(type);
    }

    private static ITutorialAction CreateSpawnDoorAction(Vec2f pos, string bossTypeStr)
    {
        var parseSuccess = Enum.TryParse<BossType>(bossTypeStr, ignoreCase: true, out var bossType);
        Debug.Assert(parseSuccess, $"不正な BossType です: {bossTypeStr}");
        if (!parseSuccess) bossType = BossType.BossSnake;

        return new SpawnDoorAction(pos, bossType);
    }
}
