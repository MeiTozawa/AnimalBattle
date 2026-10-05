using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     自機コントローラー <see cref="PlayerController"/> の入力収集および意図伝播テスト。
/// </summary>
public class PlayerControllerTests
{
    [Fact]
    public void PlayerController_InjectInput_PopulatesEntityIntentsCorrectly()
    {
        using var world = World.Create();
        var entity = world.Entity("PlayerTest");
        var player = new PlayerPawn(entity, new PlayerConfig { MaxHp = 3, MaxJumps = 2 }, -1);
        var controller = new PlayerController(player);

        Assert.Same(player, controller.Pawn);

        controller.SubmitInput(moveX: -1.0f, jumpPressed: true, jumpHolding: true, shootPressed: true);

        ref readonly var moveIntent = ref entity.Get<MovementIntent>();
        ref readonly var weaponIntent = ref entity.Get<WeaponIntent>();

        Assert.Equal(-1.0f, moveIntent.MoveX);
        Assert.True(moveIntent.JumpTriggered);
        Assert.True(moveIntent.JumpHolding);
        Assert.True(weaponIntent.ShootTriggered);
    }

    [Fact]
    public void PlayerController_InjectInput_WithCustomAimDirection_SetsWeaponIntentDirection()
    {
        using var world = World.Create();
        var entity = world.Entity("PlayerAimTest");
        var player = new PlayerPawn(entity, new PlayerConfig { MaxHp = 3, MaxJumps = 2 }, -1);
        var controller = new PlayerController(player);

        controller.SubmitInput(moveX: 0.0f, jumpPressed: false, jumpHolding: false, shootPressed: true, aimDirX: 0.0f, aimDirY: -1.0f);

        ref readonly var weaponIntent = ref entity.Get<WeaponIntent>();
        Assert.True(weaponIntent.ShootTriggered);
        Assert.Equal(0.0f, weaponIntent.AimDirectionX);
        Assert.Equal(-1.0f, weaponIntent.AimDirectionY);
    }
}
