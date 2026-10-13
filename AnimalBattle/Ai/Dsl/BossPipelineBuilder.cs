using AnimalBattle.Abilities.Attack;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     Boss の振る舞い・技能パイプラインを宣言的に構築する最上位ビルダーです。
/// </summary>
/// <param name="bossType">Boss 種別。</param>
/// <param name="name">パイプラインの識別名。</param>
public sealed class BossPipelineBuilder(BossType bossType, string name) {
    private readonly List<PhasePipelineBuilder> _phaseBuilders = [];


    /// <summary>
    ///     新しい Phase の定義を開始します。
    /// </summary>
    /// <param name="phase">対象の Phase。</param>
    /// <returns>新しい Phase ビルダー。</returns>
    public PhasePipelineBuilder Phase(Phase phase)
    {
        PhasePipelineBuilder builder = new(this, phase);
        _phaseBuilders.Add(builder);
        return builder;
    }

    /// <summary>
    ///     定義された全 Phase と攻撃パターンから不変の BossBehaviorPipeline を構築します。
    /// </summary>
    /// <returns>構築された BossBehaviorPipeline。</returns>
    public BossBehaviorPipeline Build()
    {
        Dictionary<Phase, IReadOnlyList<AttackDecisionRule>> rulesByPhase = [];
        Dictionary<AbilityId, AttackDefinition> attacks = [];
        Dictionary<Phase, PhaseLocomotionSettings> locomotionByPhase = [];

        foreach (var phaseBuilder in _phaseBuilders)
        {
            locomotionByPhase[phaseBuilder.CurrentPhase] = phaseBuilder.LocomotionSettings;
            List<AttackDecisionRule> phaseRules = [];
            foreach (var attackBuilder in phaseBuilder.AttackBuilders)
            {
                var def = attackBuilder.BuildAttackDefinition();
                attacks[attackBuilder.Skill] = def;

                var rule = attackBuilder.BuildDecisionRule();
                if (rule.HasValue) phaseRules.Add(rule.Value);
            }

            rulesByPhase[phaseBuilder.CurrentPhase] = phaseRules.AsReadOnly();
        }

        return new BossBehaviorPipeline(bossType, name, rulesByPhase, attacks, locomotionByPhase);
    }
}