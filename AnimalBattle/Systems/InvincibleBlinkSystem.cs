using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     無敵時間（Untargetable タグ）中のキャラクターの点滅演出（Sprite.IsVisible の周期反転）を制御する ECS システム。
///     描画システム側での AbilitySystemComponent 探査を排除し、純粋なデータ駆動で点滅を制御します。
/// </summary>
public static class InvincibleBlinkSystem {
    /// <summary>
    ///     無敵点滅システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddInvincibleBlinkSystem(this World world)
    {
        world.System<Sprite, AbilitySystemComponent>("InvincibleBlinkSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<Sprite> sprites, Field<AbilitySystemComponent> abilities) =>
            {
                var count = it.Count();
                var blinkFreq = ConfigManager.Render.Player.InvincibleBlinkFrequency;

                for (var i = 0; i < count; i++)
                {
                    var ability = abilities[i];
                    var untargetableTimer = ability.GetTagRemainingTime(GameplayTag.Untargetable);

                    ref var sprite = ref sprites[i];
                    if (untargetableTimer > 0.0f)
                    {
                        sprite.IsVisible = (int)(untargetableTimer * blinkFreq) % 2 == 0;
                    }
                    else
                    {
                        sprite.IsVisible = true;
                    }
                }
            });

        return world;
    }
}
