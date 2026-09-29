using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using Xunit;

namespace AnimalBattle.Tests.Ai;

public class FlattenedAiStateModuleTests
{
    [Fact]
    public void FixedBossStateModuleCatalog_IsCompleteAndValid()
    {
        bool isComplete = FixedBossStateModuleCatalog.IsComplete(out string diagnostic);
        Assert.True(isComplete, diagnostic);
    }

    [Fact]
    public void FixedBossStateModuleCatalog_ResolvesConcreteDomainModules()
    {
        (BossState State, Type ExpectedType)[] expectations =
        [
            (BossState.Boss1_Hover, typeof(HoverMovementModule)),
            (BossState.Boss1_AttackSelection, typeof(Boss1AttackSelectionModule)),
            (BossState.Boss1_Telegraph, typeof(Boss1TelegraphModule)),
            (BossState.Boss1_Execute, typeof(Boss1ExecuteModule)),
            (BossState.Boss1_Recovery, typeof(Boss1RecoveryModule)),
            (BossState.Boss1_PhaseTransition, typeof(Boss1PhaseTransitionModule)),
            (BossState.Boss1_Dead, typeof(Boss1DeadModule)),

            (BossState.Boss2_Idle, typeof(Boss2IdleModule)),
            (BossState.Boss2_PatrolChase, typeof(Boss2PatrolChaseModule)),
            (BossState.Boss2_MoveToTakeoff, typeof(Boss2MoveToTakeoffModule)),
            (BossState.Boss2_PlatformTraverse, typeof(Boss2PlatformTraverseModule)),
            (BossState.Boss2_Telegraph, typeof(Boss2TelegraphModule)),
            (BossState.Boss2_Execute, typeof(Boss2ExecuteModule)),
            (BossState.Boss2_WallCrashStun, typeof(Boss2WallCrashStunModule)),
            (BossState.Boss2_Recovery, typeof(Boss2RecoveryModule)),
            (BossState.Boss2_PhaseTransition, typeof(Boss2PhaseTransitionModule)),
            (BossState.Boss2_Dead, typeof(Boss2DeadModule))
        ];

        foreach (var (state, expectedType) in expectations)
        {
            bool found = FixedBossStateModuleCatalog.Get(state, out FixedBossStateModule entry);
            Assert.True(found, $"Module entry for state {state} not found in catalog");
            Assert.Equal(expectedType, entry.ModuleType);
            Assert.True(entry.IsValid, $"Module entry for state {state} is invalid");
        }
    }
}
