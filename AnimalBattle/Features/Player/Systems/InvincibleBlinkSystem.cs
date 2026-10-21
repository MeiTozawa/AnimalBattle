using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Player.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     無敵時間（Untargetable タグ）中のキャラクターの点滅演出（Sprite.IsVisible の周期反転）を制御する ECS システム。
///     エッジトリガー方式の状態追跡により、無敵以外の通常状態や演出・死亡時の可視性強制上書きを防止します。
/// </summary>
public static class InvincibleBlinkSystem {
    /// <summary>
    ///     無敵点滅システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddInvincibleBlinkSystem(this World world)
    {
        world.System<Sprite, TimedEffectsState, GameplayTagState, InvincibleBlinkState>("InvincibleBlinkSystem")
            .Without<DeadTag>()
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteInvincibleBlink);

        return world;
    }

    private static void ExecuteInvincibleBlink(
        Iter it,
        Field<Sprite> sprites,
        Field<TimedEffectsState> timedEffects,
        Field<GameplayTagState> tags,
        Field<InvincibleBlinkState> blinkStates)
    {
        var count = it.Count();
        if (count == 0) return;

        var blinkFreq = it.World().Get<RenderConfig>().Player.InvincibleBlinkFrequency;

        for (var i = 0; i < count; i++)
        {
            ref readonly var tag = ref tags[i];
            ref var blinkState = ref blinkStates[i];
            ref var sprite = ref sprites[i];

            var isUntargetable = tag.HasTag(GameplayTag.Untargetable);

            if (isUntargetable)
            {
                // 点滅状態を記録し、周期的に可視性を反転
                blinkState.IsBlinking = true;
                ref readonly var effect = ref timedEffects[i];
                var untargetableTimer = TimedEffectService.GetTagRemainingTime(in effect, GameplayTag.Untargetable);
                sprite.IsVisible = (int)(untargetableTimer * blinkFreq) % 2 == 0;
            }
            else if (blinkState.IsBlinking)
            {
                // エッジトリガー：無敵解除の瞬間にのみ1度だけ可視状態へ復元
                blinkState.IsBlinking = false;
                sprite.IsVisible = true;
            }
            // 通常エンティティ（IsBlinking == false）：外部の不可視・演出・死亡状態を維持するため Sprite.IsVisible に一切触れない
        }
    }
}
