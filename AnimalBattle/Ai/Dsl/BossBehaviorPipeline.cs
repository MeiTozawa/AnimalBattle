using System.Collections.Generic;
using System.Collections.Frozen;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// 宣言的 DSL から一括構築された不変の Boss 振る舞い・技能パイプラインです。
/// </summary>
public sealed class BossBehaviorPipeline
{
    private readonly BossType _bossType;
    private readonly string _name;
    private readonly FrozenDictionary<Phase, IReadOnlyList<AttackDecisionRule>> _rulesByPhase;
    private readonly FrozenDictionary<AttackId, AttackDefinition> _attacks;

    /// <summary>Boss の種別を取得します。</summary>
    public BossType BossType => _bossType;

    /// <summary>パイプラインの識別名を取得します。</summary>
    public string Name => _name;

    /// <summary>全攻撃の検証済み AttackDefinition マップを取得します。</summary>
    public IReadOnlyDictionary<AttackId, AttackDefinition> Attacks => _attacks;

    /// <summary>
    /// パイプラインを初期化します。
    /// </summary>
    public BossBehaviorPipeline(
        BossType bossType,
        string name,
        Dictionary<Phase, IReadOnlyList<AttackDecisionRule>> rulesByPhase,
        Dictionary<AttackId, AttackDefinition> attacks)
    {
        _bossType = bossType;
        _name = name;
        _rulesByPhase = rulesByPhase.ToFrozenDictionary();
        _attacks = attacks.ToFrozenDictionary();
    }

    /// <summary>
    /// 指定 Phase の攻撃意思決定ルール一覧を取得します。
    /// </summary>
    /// <param name="phase">対象の Phase。</param>
    /// <returns>意思決定ルールの読み取り専用リスト。</returns>
    public IReadOnlyList<AttackDecisionRule> GetRules(Phase phase)
    {
        return _rulesByPhase.TryGetValue(phase, out IReadOnlyList<AttackDecisionRule>? rules)
            ? rules
            : [];
    }

    /// <summary>
    /// 指定された AttackId の AttackDefinition を取得します。
    /// </summary>
    /// <param name="attackId">攻撃識別子。</param>
    /// <param name="definition">見つかった攻撃定義。</param>
    /// <returns>定義が存在する場合は true。</returns>
    public bool TryGetAttack(AttackId attackId, out AttackDefinition definition)
    {
        return _attacks.TryGetValue(attackId, out definition!);
    }
}
