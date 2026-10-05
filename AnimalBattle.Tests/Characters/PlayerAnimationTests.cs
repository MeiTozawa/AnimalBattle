using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     自機アニメーション状態選択システム (PlayerAnimationStateSystem) の単体テスト。
/// </summary>
public class PlayerAnimationTests
{
    [Fact]
    public void PlayerAnimationStateSystem_SelectsAppropriateClips_BasedOnVelocityAndGroundState()
    {
        using var world = World.Create();
        world.AddPlayerAnimationStateSystem();

        var player = world.Entity("TestPlayer")
            .Add<PlayerTag>()
            .Set(new Velocity(0.0f, 0.0f))
            .Set(new CollisionBox(32.0f, 32.0f) { IsGrounded = true })
            .Set(new Health(3.0f))
            .Set(new Animation("Idle"));

        world.Progress(0.016f);
        Assert.Equal("Idle", player.Get<Animation>().CurrentClip);

        // 走行クリップの選択
        player.GetMut<Velocity>().Vx = 150.0f;
        world.Progress(0.016f);
        Assert.Equal("Run", player.Get<Animation>().CurrentClip);

        // ジャンプ上昇クリップの選択
        player.GetMut<CollisionBox>().IsGrounded = false;
        player.GetMut<Velocity>().Vy = -200.0f;
        world.Progress(0.016f);
        Assert.Equal("Jump", player.Get<Animation>().CurrentClip);

        // 落下クリップの選択
        player.GetMut<Velocity>().Vy = 200.0f;
        world.Progress(0.016f);
        Assert.Equal("Fall", player.Get<Animation>().CurrentClip);
    }

    [Fact]
    public void PlayerAnimationStateSystem_IgnoresDeadPlayer()
    {
        using var world = World.Create();
        world.AddPlayerAnimationStateSystem();

        var player = world.Entity("DeadPlayer")
            .Add<PlayerTag>()
            .Add<DeadTag>()
            .Set(new Velocity(150.0f, 0.0f))
            .Set(new CollisionBox(32.0f, 32.0f) { IsGrounded = true })
            .Set(new Health(0.0f))
            .Set(new Animation("Death"));

        world.Progress(0.016f);
        Assert.Equal("Death", player.Get<Animation>().CurrentClip);
    }

    [Fact]
    public void PlayerAnimationStateSystem_IgnoresNonPlayerEntities()
    {
        using var world = World.Create();
        world.AddPlayerAnimationStateSystem();

        var enemy = world.Entity("Enemy")
            .Set(new Velocity(150.0f, 0.0f))
            .Set(new CollisionBox(32.0f, 32.0f) { IsGrounded = true })
            .Set(new Health(10.0f))
            .Set(new Animation("Special"));

        world.Progress(0.016f);
        Assert.Equal("Special", enemy.Get<Animation>().CurrentClip);
    }
}
