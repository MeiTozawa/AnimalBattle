using AnimalBattle.Components;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     キャラクター制御意図コンポーネント（MovementIntent / WeaponIntent）のテスト。
/// </summary>
public class CharacterIntentTests
{
    [Fact]
    public void MovementIntent_Reset_ClearsAllInputs()
    {
        var intent = new MovementIntent
        {
            MoveX = 1.0f,
            JumpTriggered = true,
            JumpHolding = true
        };

        intent.Reset();

        Assert.Equal(0.0f, intent.MoveX);
        Assert.False(intent.JumpTriggered);
        Assert.False(intent.JumpHolding);
    }

    [Fact]
    public void WeaponIntent_Reset_ClearsFireState()
    {
        var intent = new WeaponIntent
        {
            ShootTriggered = true,
            AimDirectionX = 1.0f,
            AimDirectionY = 0.0f
        };

        intent.Reset();

        Assert.False(intent.ShootTriggered);
        Assert.Equal(0.0f, intent.AimDirectionX);
        Assert.Equal(0.0f, intent.AimDirectionY);
    }
}
