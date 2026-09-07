using System;
using System.Collections.Generic;
using System.Drawing;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// 単一の攻撃パターン（前摇、多段時間軸、後隙、意思決定ルール）を宣言的に構築するビルダーです。
/// </summary>
/// <param name="parent">親の Phase ビルダー。</param>
/// <param name="skill">技能識別子。</param>
public sealed class AttackPipelineBuilder(PhasePipelineBuilder parent, BossSkill skill)
{
    private readonly PhasePipelineBuilder _parent = parent;
    private readonly BossSkill _skill = skill;
    private readonly List<AttackComponent> _components = [];
    private float _telegraphDuration = 0.40f;
    private Color _telegraphColor = ColorPalette.Red;
    private float _executeDuration = 0.50f;
    private float _recoveryDuration = 0.50f;
    private float _range = 200.0f;
    private DecisionRuleBuilder? _ruleBuilder;

    /// <summary>対象の技能識別子を取得します。</summary>
    public BossSkill Skill => _skill;

    /// <summary>
    /// 攻撃の前摇（Telegraph）時間と予兆色を設定します。
    /// </summary>
    /// <param name="duration">前摇時間（秒）。</param>
    /// <param name="color">予兆カラー。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Telegraph(float duration, Color color)
    {
        _telegraphDuration = duration;
        _telegraphColor = color;
        return this;
    }

    /// <summary>
    /// 攻撃の実行（Execute）時間と攻撃有効射程を設定します。
    /// </summary>
    /// <param name="duration">実行時間（秒）。</param>
    /// <param name="range">攻撃射程（px）。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Execute(float duration, float range = 200.0f)
    {
        _executeDuration = duration;
        _range = range;
        return this;
    }

    /// <summary>
    /// 指定された発動時刻（TriggerTime）におけるアクションを定義します。
    /// </summary>
    /// <param name="atTime">Execute 開始からの遅延時間（秒）。</param>
    /// <param name="configure">アクションビルダーの設定デリゲート。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Action(float atTime, Action<AttackActionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        AttackActionBuilder actionBuilder = new(atTime, _components);
        configure(actionBuilder);
        return this;
    }

    /// <summary>
    /// 接地時（着地衝突時）に発火するイベント駆動アクションを定義します。
    /// </summary>
    /// <param name="configure">アクションビルダーの設定デリゲート。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder OnGrounded(Action<AttackActionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        AttackActionBuilder actionBuilder = new(0.0f, _components, isOnGrounded: true);
        configure(actionBuilder);
        return this;
    }

    /// <summary>
    /// 攻撃の後隙（Recovery）時間を設定します。
    /// </summary>
    /// <param name="duration">後隙時間（秒）。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Recovery(float duration)
    {
        _recoveryDuration = duration;
        return this;
    }

    /// <summary>
    /// この攻撃の意思決定ルール（AI 発動条件・クールダウン等）を定義します。
    /// </summary>
    /// <param name="configure">意思決定ルールビルダーの設定デリゲート。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Rule(Action<DecisionRuleBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _ruleBuilder = new();
        configure(_ruleBuilder);
        return this;
    }

    /// <summary>
    /// TOML 設定オブジェクトから意思決定ルール（AI 発動条件・クールダウン等）を適用します。
    /// </summary>
    /// <param name="config">TOML から読み込まれたルール設定オブジェクト。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Rule(AttackRuleConfig? config)
    {
        if (config is null)
        {
            return this;
        }

        _ruleBuilder = new DecisionRuleBuilder()
            .Cooldown(config.Cooldown)
            .Distance(config.MinDistance, config.MaxDistance)
            .Weight(config.Weight)
            .RequireSamePlatform(config.RequireSamePlatform)
            .PreferAirborneBonus(config.PreferAirborneBonus);

        if (config.RequirePlayerGrounded.HasValue)
        {
            _ruleBuilder.RequirePlayerGrounded(config.RequirePlayerGrounded.Value);
        }

        return this;
    }

    /// <summary>
    /// TOML 設定のルールリストから、このビルダーの技能識別子 (Skill) に合致するルールを自動検索して適用します。
    /// </summary>
    /// <param name="rules">TOML から読み込まれたルール設定リスト。</param>
    /// <returns>現在の攻撃ビルダー。</returns>
    public AttackPipelineBuilder Rule(List<AttackRuleConfig>? rules)
    {
        if (rules is null)
        {
            return this;
        }

        string skillName = _skill.ToString();
        AttackRuleConfig? config = rules.Find(r => string.Equals(r.Skill, skillName, StringComparison.OrdinalIgnoreCase));
        return Rule(config);
    }

    /// <summary>
    /// 次の攻撃の定義に進みます（親の PhasePipelineBuilder へ戻ります）。
    /// </summary>
    /// <param name="skill">次に定義する技能識別子。</param>
    /// <returns>新しい攻撃ビルダー。</returns>
    public AttackPipelineBuilder Attack(BossSkill skill) => _parent.Attack(skill);

    /// <summary>
    /// 次の Phase の定義に進みます。
    /// </summary>
    /// <param name="phase">次に定義する Phase。</param>
    /// <returns>新しい Phase ビルダー。</returns>
    public PhasePipelineBuilder Phase(Phase phase) => _parent.Phase(phase);

    /// <summary>
    /// 全体のパイプラインをビルドして終了します。
    /// </summary>
    /// <returns>構築された BossBehaviorPipeline。</returns>
    public BossBehaviorPipeline Build() => _parent.Build();

    /// <summary>
    /// 設定されたパラメータから不変の AttackDefinition を生成します。
    /// </summary>
    /// <returns>検証済みの AttackDefinition。</returns>
    public AttackDefinition BuildAttackDefinition()
    {
        return new AttackDefinition(
            _skill,
            _telegraphDuration,
            _executeDuration,
            _recoveryDuration,
            _range,
            _components.ToArray());
    }

    /// <summary>
    /// 設定された意思決定ルールを生成します。未定義の場合は null を返します。
    /// </summary>
    /// <returns>生成された AttackDecisionRule、または null。</returns>
    public AttackDecisionRule? BuildDecisionRule() => _ruleBuilder?.Build(_skill);
}

