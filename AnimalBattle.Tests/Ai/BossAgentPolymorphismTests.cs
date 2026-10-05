using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Configs;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Ai;

/// <summary>
///     多態 Boss 自治エージェント（Boss1Agent / Boss2Agent）のポリモーフィズムおよび Pawn 継承検証テスト。
/// </summary>
public sealed class BossAgentPolymorphismTests
{
    [Fact]
    public void BossAgents_InheritFromPawn_AndBaseBossAgent()
    {
        using var world = World.Create();
        world.Import<AnimalBattle.Modules.ComponentRequirementsModule>();
        ConfigManager.Initialize();

        var b1Entity = world.Entity("Boss1Test");
        var b2Entity = world.Entity("Boss2Test");

        using var boss1 = new Boss1Agent(b1Entity, 1);
        using var boss2 = new Boss2Agent(b2Entity, 2, () => null);

        Assert.True(boss1 is Pawn);
        Assert.True(boss1 is BaseBossAgent);
        Assert.Equal(BossType.Boss1, boss1.BossType);

        Assert.True(boss2 is Pawn);
        Assert.True(boss2 is BaseBossAgent);
        Assert.Equal(BossType.Boss2, boss2.BossType);

        // Pawn 基底クラスからの血量アクセサ検証
        Assert.True(boss1.CurrentHp > 0);
        Assert.True(boss2.CurrentHp > 0);

        // Pawn 基底クラスの双方向速度・位置操作検証
        boss1.SetPosition(400.0f, 300.0f);
        Assert.Equal(400.0f, boss1.X);
        Assert.Equal(300.0f, boss1.Y);
    }
}
