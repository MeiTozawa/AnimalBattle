using AnimalBattle.Characters;
using AnimalBattle.Components;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

public class CharacterContractTests
{
    private sealed class DummyCharacter(Entity entity) : ICharacter
    {
        public Entity Entity { get; } = entity;
    }

    [Fact]
    public void ICharacter_DefaultImplementation_DirectlyAccessesComponents_WithoutHasOverhead()
    {
        using var world = World.Create();
        var entity = world.Entity("TestPawn")
            .Set(new Transform(120.0f, 240.0f, 1.0f, 1.0f))
            .Set(new Velocity(50.0f, -80.0f))
            .Set(new Health(100.0f, 75.0f) { InvincibleTimer = 0.5f })
            .Set(new Hurtbox(new CollisionShape.Box(32.0f, 32.0f), Faction.Hostile));

        ICharacter character = new DummyCharacter(entity);

        // デフォルト実装によって Has 検査なしで直接値が取得されることを検証
        Assert.Equal((ulong)entity.Id, character.EntityId);
        Assert.Equal(120.0f, character.X);
        Assert.Equal(240.0f, character.Y);
        Assert.Equal(50.0f, character.Vx);
        Assert.Equal(-80.0f, character.Vy);
        Assert.Equal(75.0f, character.CurrentHp);
        Assert.Equal(100.0f, character.MaxHp);
        Assert.True(character.IsAlive);
        Assert.True(character.IsInvincible);
        Assert.Equal(Faction.Hostile, character.Faction);
    }

    [Fact]
    public void PlayerAndBoss_DoNotDeclareDuplicateICharacterProperties()
    {
        // 派生クラスで ICharacter のデフォルト実装プロパティを再定義（重複定義）していないことを検証
        var playerProps = typeof(Player).GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
#pragma warning disable CS0618
        var bossProps = typeof(BossCharacter).GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
#pragma warning restore CS0618
        var baseBossProps = typeof(AnimalBattle.Characters.Boss.BaseBossController).GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var b1Props = typeof(AnimalBattle.Characters.Boss.Boss1Controller).GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var b2Props = typeof(AnimalBattle.Characters.Boss.Boss2Controller).GetProperties(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        string[] duplicateCandidates = ["Sprite", "ScaleX", "ScaleY", "Animation", "IsFlashing", "EntityId", "Faction", "CurrentHp", "MaxHp", "IsAlive", "IsInvincible"];

        foreach (var name in duplicateCandidates)
        {
            Assert.DoesNotContain(playerProps, p => p.Name == name);
            Assert.DoesNotContain(bossProps, p => p.Name == name);
            Assert.DoesNotContain(baseBossProps, p => p.Name == name);
            Assert.DoesNotContain(b1Props, p => p.Name == name);
            Assert.DoesNotContain(b2Props, p => p.Name == name);
        }
    }
}
