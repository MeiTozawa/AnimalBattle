using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Ecs;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 固定 Boss AI runtime を一つの ECS system として登録します。
/// </summary>
public static class BossAiRuntimeRegistration
{
    /// <summary>
    /// Boss1 と Boss2 の旧 AI system 二登録を置き換える runtime system を登録します。
    /// </summary>
    /// <param name="world">登録先の ECS World です。</param>
    /// <returns>メソッドチェーン用の World です。</returns>
    public static World AddBossAiRuntimeSystem(this World world)
    {
        ObservationService observation = new();
        ConfigProvider configProvider = ConfigProvider.FromConfigManager(observation: observation);
        EnumModuleMap moduleMap = CreateModuleMap(observation);
        TransitionService transitionService = new(moduleMap, observation);
        BossAiRuntime runtime = new(moduleMap, transitionService, observation);
        BossAiRuntimeSystem runtimeSystem = new(
            world,
            runtime,
            (entityId, bossType) => CreateRuntimeSlot(entityId, bossType, configProvider, observation),
            () => new BossEcsUpdateGate(configProvider.Current.IsValid, moduleMap.RuntimeReady));

        world.System("BossAiRuntimeSystem")
             .Kind(Ecs.OnUpdate)
             .Run((Iter iteration) => runtimeSystem.Update(iteration.DeltaTime()));

        return world;
    }

    private static EnumModuleMap CreateModuleMap(ObservationService observation)
    {
        Boss1DefaultConfiguration boss1Configuration = new();
        Boss2DefaultConfiguration boss2Configuration = new();
        Boss1BehaviorState boss1State = new(0.0f, 0.0f);
        Boss2BehaviorState boss2State = new();

        IBossStateModule[] modules =
        [
            new HoverMovementModule(boss1Configuration, boss1State),
            new Boss1AttackSelectionModule(boss1Configuration, boss1State),
            new Boss1TelegraphModule(boss1Configuration, boss1State),
            new Aimed3ShotExecuteModule(boss1Configuration, boss1State),
            new RingBurstExecuteModule(boss1Configuration, boss1State),
            new DoubleSpiralExecuteModule(boss1Configuration, boss1State),
            new RainDropExecuteModule(boss1Configuration, boss1State),
            new CrossChaosExecuteModule(boss1Configuration, boss1State),
            new FastSniperExecuteModule(boss1Configuration, boss1State),
            new DiveRamExecuteModule(boss1Configuration, boss1State),
            new Boss1RecoveryModule(boss1Configuration, boss1State),
            new Boss1PhaseTransitionModule(boss1Configuration, boss1State),
            new Boss1DeadModule(boss1State),

            new Boss2IdleModule(boss2State),
            new Boss2PatrolChaseModule(boss2Configuration, boss2State),
            new Boss2MoveToTakeoffModule(boss2Configuration, boss2State),
            new Boss2PlatformJumpModule(boss2Configuration, boss2State),
            new Boss2PlatformFallModule(boss2Configuration, boss2State),
            new Boss2LandingRecoveryModule(boss2State),
            new Boss2HeavySlashTelegraphModule(boss2Configuration, boss2State),
            new Boss2DoubleCleaveTelegraphModule(boss2Configuration, boss2State),
            new Boss2GroundStompTelegraphModule(boss2Configuration, boss2State),
            new Boss2LeapSmashTelegraphModule(boss2Configuration, boss2State),
            new Boss2FrenzyChargeTelegraphModule(boss2Configuration, boss2State),
            new Boss2EarthquakeRoarTelegraphModule(boss2Configuration, boss2State),
            new Boss2HeavySlashExecuteModule(boss2Configuration, boss2State),
            new Boss2DoubleCleaveExecuteModule(boss2Configuration, boss2State),
            new Boss2GroundStompExecuteModule(boss2Configuration, boss2State),
            new Boss2LeapSmashAscentModule(boss2Configuration, boss2State),
            new Boss2LeapSmashDescentModule(boss2Configuration, boss2State),
            new Boss2FrenzyChargeExecuteModule(boss2Configuration, boss2State),
            new Boss2EarthquakeRoarExecuteModule(boss2Configuration, boss2State),
            new Boss2WallCrashStunModule(boss2State),
            new Boss2RecoveryModule(boss2Configuration, boss2State),
            new Boss2PhaseTransitionModule(boss2Configuration, boss2State),
            new Boss2DeadModule(boss2State)
        ];
        string[] dependencies =
        [
            "AttackPort",
            "Boss1Configuration",
            "Boss2Configuration",
            "EffectPort",
            "MovementPort",
            "ObservationService",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies, observation);
    }

    private static BossRuntimeSlot CreateRuntimeSlot(
        ulong entityId,
        AnimalBattle.Ai.Contracts.BossType bossType,
        ConfigProvider configProvider,
        ObservationService observation)
    {
        PreallocatedCommandBuffer<MovementCommand> movementCommands = new(32);
        PreallocatedCommandBuffer<AttackCommand> attackCommands = new(32);
        PreallocatedCommandBuffer<EffectCommand> effectCommands = new(16);
        IMovementPort movement = bossType == AnimalBattle.Ai.Contracts.BossType.Boss1
            ? new Boss1MovementPort(movementCommands)
            : new Boss2MovementPort(movementCommands);

        return new BossRuntimeSlot(
            entityId,
            bossType,
            configProvider,
            movement,
            new BufferedAttackPort(attackCommands),
            new BufferedEffectPort(effectCommands),
            observation,
            new SystemRandomSource(),
            debugEnabled: ConfigManager.Render.Debug.Enabled,
            movementCommandBuffer: movementCommands,
            attackCommandBuffer: attackCommands,
            effectCommandBuffer: effectCommands);
    }

    private sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random = new();

        public int NextInt(int exclusiveMax)
        {
            return _random.Next(exclusiveMax);
        }

        public float NextSingle()
        {
            return _random.NextSingle();
        }
    }
}
