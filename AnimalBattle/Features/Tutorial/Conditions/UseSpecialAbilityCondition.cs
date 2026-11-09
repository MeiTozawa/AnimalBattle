using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Systems;

namespace AnimalBattle.Features.Tutorial.Conditions;

/// <summary>
///     プレイヤーが特殊能力を発動し、かつその効果がフィールド上から完全に消失して
///     一定の猶予時間（余韻）が経過したかを判定する条件。
/// </summary>
public sealed class UseSpecialAbilityCondition(float postClearDelay = 0.8f) : ITutorialCondition
{
    private readonly float _postClearDelay = postClearDelay;

    /// <inheritdoc />
    public bool IsSatisfied(World world, ref TutorialState state, float dt)
    {
        // 1. 特殊能力がまだ発動されていない場合は未達成
        if (!state.HasUsedSpecialAbility)
        {
            state.StepCompletionTimer = _postClearDelay;
            return false;
        }

        // 2. 特殊能力エンティティ（巨型エネルギー弾または全画面連続斬撃）がまだ場に残存している場合は待機
        var ctx = world.Get<TutorialSystemContext>();
        if (ctx.ActiveSpecialAbilityQuery.Count() > 0)
        {
            state.StepCompletionTimer = _postClearDelay;
            return false;
        }

        // 3. 特殊能力の消失後、小怪出現前のクールダウン・余韻タイマーをカウントダウン
        if (state.StepCompletionTimer > 0f)
        {
            state.StepCompletionTimer -= dt;
            if (state.StepCompletionTimer > 0f)
            {
                return false;
            }
        }

        return true;
    }
}
