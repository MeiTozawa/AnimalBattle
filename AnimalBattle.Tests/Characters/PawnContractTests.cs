using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     Pawn 基類のゼロ Has アクセサ、双方向位置・速度設定、および共通ライフサイクルメソッドの検証テスト。
/// </summary>
public sealed class PawnContractTests
{
    private sealed class TestPawn : Pawn
    {
        public TestPawn(Entity entity) : base(entity) { }
    }

    [Fact]
    public void Pawn_ExposesAndMutatesComponentDataDirectly_WithoutHasChecks()
    {
        using var world = World.Create();
        var entity = world.Entity()
            .Set(new Transform(100.0f, 200.0f, 1.5f, 1.5f))
            .Set(new Velocity(50.0f, -30.0f))
            .Set(new Health(100.0f, 80.0f) { InvincibleTimer = 0.5f })
            .Set(new CollisionBox(64.0f, 64.0f))
            .Set(new Hurtbox(new CollisionShape.Circle(20.0f), Faction.Hostile))
            .Set(new Sprite(1, false, true, new Rect(0, 0, 64, 64)))
            .Set(new Animation("Idle"));

        var pawn = new TestPawn(entity);

        // 1. 只读访问器验证
        Assert.Equal((ulong)entity.Id, pawn.EntityId);
        Assert.Equal(100.0f, pawn.X);
        Assert.Equal(200.0f, pawn.Y);
        Assert.Equal(50.0f, pawn.Vx);
        Assert.Equal(-30.0f, pawn.Vy);
        Assert.Equal(80.0f, pawn.CurrentHp);
        Assert.Equal(100.0f, pawn.MaxHp);
        Assert.True(pawn.IsAlive);
        Assert.True(pawn.IsInvincible);
        Assert.Equal(Faction.Hostile, pawn.Faction);
        Assert.Equal(1.5f, pawn.ScaleX);
        Assert.Equal(1.5f, pawn.ScaleY);
        Assert.False(pawn.IsGrounded);
        Assert.False(pawn.FlipX);
        Assert.True(pawn.IsVisible);
        Assert.False(pawn.IsFlashing);
        Assert.Equal("Idle", pawn.CurrentAnimation);

        // 2. 双向属性修改与快捷方法验证
        pawn.X = 150.0f;
        pawn.Y = 250.0f;
        Assert.Equal(150.0f, entity.Get<Transform>().X);
        Assert.Equal(250.0f, entity.Get<Transform>().Y);

        pawn.SetVelocity(120.0f, -80.0f);
        Assert.Equal(120.0f, pawn.Vx);
        Assert.Equal(-80.0f, pawn.Vy);

        pawn.IsGrounded = true;
        Assert.True(pawn.IsGrounded);

        pawn.FlipX = true;
        Assert.True(pawn.FlipX);

        pawn.ResetHealth(120.0f);
        Assert.Equal(120.0f, pawn.CurrentHp);
        Assert.Equal(120.0f, pawn.MaxHp);
        Assert.False(pawn.IsInvincible);

        // 3. 瞬态效果清理验证
        entity.Set(new HitFlashEffect(0.1f));
        entity.Add<DeadTag>();
        Assert.True(pawn.IsFlashing);

        pawn.ClearTransientEffects();
        Assert.False(pawn.IsFlashing);
        Assert.False(entity.Has<DeadTag>());
    }
}
