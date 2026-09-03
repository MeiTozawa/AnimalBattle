using System;

namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 技能の意思決定・出招判定条件を定義する検証済みルール構造体です。
/// </summary>
public readonly struct AttackDecisionRule
{
    /// <summary>対象の技能識別子を取得します。</summary>
    public BossSkill Skill { get; }

    /// <summary>技能の基本冷却時間（秒）を取得します。</summary>
    public float Cooldown { get; }

    /// <summary>技能を発動可能な最小有効距離（ピクセル）を取得します。</summary>
    public float MinDistance { get; }

    /// <summary>技能を発動可能な最大有効距離（ピクセル）を取得します。</summary>
    public float MaxDistance { get; }

    /// <summary>意思決定時の基礎加重（欲望値）を取得します。</summary>
    public float Weight { get; }

    /// <summary>Boss と対象が同一足場に存在することを要求するかどうかを取得します。</summary>
    public bool RequireSamePlatform { get; }

    /// <summary>対象プレイヤーが接地中であることを要求するかどうかを取得します（null の場合は問わない）。</summary>
    public bool? RequirePlayerGrounded { get; }

    /// <summary>対象プレイヤーが空中にいる場合の加重倍率を取得します（既定値: 1.0）。</summary>
    public float PreferAirborneBonus { get; }

    /// <summary>ルール設定が有効かどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
        && Cooldown >= 0.0f
        && MinDistance >= 0.0f
        && MaxDistance >= MinDistance
        && Weight > 0.0f
        && PreferAirborneBonus > 0.0f
        && !float.IsNaN(Cooldown)
        && !float.IsNaN(MinDistance)
        && !float.IsNaN(MaxDistance)
        && !float.IsNaN(Weight)
        && !float.IsNaN(PreferAirborneBonus);

    /// <summary>
    /// 技能意思決定ルールを初期化します。
    /// </summary>
    /// <param name="skill">技能識別子。</param>
    /// <param name="cooldown">基本冷却時間（秒）。</param>
    /// <param name="minDistance">最小有効距離。</param>
    /// <param name="maxDistance">最大有効距離。</param>
    /// <param name="weight">基礎加重。</param>
    /// <param name="requireSamePlatform">同一足場を要求するかどうか。</param>
    /// <param name="requirePlayerGrounded">接地を要求するかどうか。</param>
    /// <param name="preferAirborneBonus">空中ボーナス倍率。</param>
    public AttackDecisionRule(
        BossSkill skill,
        float cooldown = 0.0f,
        float minDistance = 0.0f,
        float maxDistance = 300.0f,
        float weight = 100.0f,
        bool requireSamePlatform = false,
        bool? requirePlayerGrounded = null,
        float preferAirborneBonus = 1.0f)
    {
        Skill = skill;
        Cooldown = cooldown;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
        Weight = weight;
        RequireSamePlatform = requireSamePlatform;
        RequirePlayerGrounded = requirePlayerGrounded;
        PreferAirborneBonus = preferAirborneBonus;
    }

}
