using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     無敵時間（Untargetable タグ）中のキャラクターの点滅演出（Sprite.IsVisible の周期反転）を制御する ECS システム。
///     純粋な DOD データコンポーネントのみを参照して点滅演出を決定します。
/// </summary>
public static class InvincibleBlinkSystem {
    /// <summary>
    ///     無敵点滅システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddInvincibleBlinkSystem(this World world)
    {
        world.System<Sprite, TimedEffectsState, GameplayTagState>("InvincibleBlinkSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<Sprite> sprites, Field<TimedEffectsState> timedEffects, Field<GameplayTagState> tags) =>
            {
                var count = it.Count();
                var blinkFreq = it.World().Get<RenderConfig>().Player.InvincibleBlinkFrequency;

                for (var i = 0; i < count; i++)
                {
                    ref readonly var effect = ref timedEffects[i];
                    ref readonly var tag = ref tags[i];
                    ref var sprite = ref sprites[i];

                    if (tag.HasTag(GameplayTag.Untargetable))
                    {
                        var untargetableTimer = TimedEffectService.GetTagRemainingTime(in effect, GameplayTag.Untargetable);
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
