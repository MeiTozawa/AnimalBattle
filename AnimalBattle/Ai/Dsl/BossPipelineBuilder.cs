using System.Collections.Generic;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// Boss の振る舞い・技能パイプラインを宣言的に構築する最上位ビルダーです。
/// </summary>
public sealed class BossPipelineBuilder
{
    private readonly BossType _bossType;
    private readonly string _name;
    private readonly List<PhasePipelineBuilder> _phaseBuilders = [];

    /// <summary>Boss 種別を取得します。</summary>
    public BossType BossType => _bossType;

    /// <summary>識別名を取得します。</summary>
    public string Name => _name;

    /// <summary>
    /// 指定された Boss 種別と名前でビルダーを初期化します。
    /// </summary>
    /// <param name="bossType">Boss 種別。</param>
    /// <param name="name">パイプラインの識別名。</param>
    public BossPipelineBuilder(BossType bossType, string name)
    {
        _bossType = bossType;
        _name = name;
    }

    /// <summary>
    /// 新しい Phase の定義を開始します。
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
    /// 定義された全 Phase と攻撃パターンから不変の BossBehaviorPipeline を構築します。
    /// </summary>
    /// <returns>構築された BossBehaviorPipeline。</returns>
    public BossBehaviorPipeline Build()
    {
        Dictionary<Phase, IReadOnlyList<AttackDecisionRule>> rulesByPhase = [];
        Dictionary<AttackId, AttackDefinition> attacks = [];

        foreach (PhasePipelineBuilder phaseBuilder in _phaseBuilders)
        {
            List<AttackDecisionRule> phaseRules = [];
            foreach (AttackPipelineBuilder attackBuilder in phaseBuilder.AttackBuilders)
            {
                AttackDefinition def = attackBuilder.BuildAttackDefinition();
                attacks[attackBuilder.AttackId] = def;

                AttackDecisionRule? rule = attackBuilder.BuildDecisionRule();
                if (rule.HasValue)
                {
                    phaseRules.Add(rule.Value);
                }
            }
            rulesByPhase[phaseBuilder.CurrentPhase] = phaseRules.AsReadOnly();
        }

        return new BossBehaviorPipeline(_bossType, _name, rulesByPhase, attacks);
    }
}
