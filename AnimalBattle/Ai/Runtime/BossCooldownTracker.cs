using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// Boss の技能ごとの残り冷却時間（Cooldown）を管理・追跡するクラスです。
/// </summary>
public sealed class BossCooldownTracker
{
    private readonly Dictionary<BossSkill, float> _cooldowns = new();

    /// <summary>
    /// 経過時間に基づいてすべての技能の冷却時間を減衰更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f || _cooldowns.Count == 0)
        {
            return;
        }

        foreach (BossSkill skill in _cooldowns.Keys)
        {
            float current = _cooldowns[skill];
            if (current > 0.0f)
            {
                _cooldowns[skill] = MathF.Max(0.0f, current - deltaTime);
            }
        }
    }

    /// <summary>
    /// 指定技能の冷却時間を設定し、クールダウンを開始します。
    /// </summary>
    /// <param name="skill">対象の技能識別子。</param>
    /// <param name="cooldown">冷却時間（秒）。</param>
    public void Trigger(BossSkill skill, float cooldown)
    {
        if (!skill.IsValid() || cooldown <= 0.0f)
        {
            return;
        }

        _cooldowns[skill] = cooldown;
    }

    /// <summary>
    /// 指定技能が使用可能（冷却完了）かどうかを判定します。
    /// </summary>
    /// <param name="skill">対象の技能識別子。</param>
    /// <returns>冷却が完了している場合は true。</returns>
    public bool IsReady(BossSkill skill)
    {
        if (!skill.IsValid())
        {
            return false;
        }

        return !_cooldowns.TryGetValue(skill, out float remaining) || remaining <= 0.0f;
    }

    /// <summary>
    /// 指定技能の残り冷却時間（秒）を取得します。
    /// </summary>
    /// <param name="skill">対象の技能識別子。</param>
    /// <returns>残り冷却時間（秒）。冷却完了時は 0.0f。</returns>
    public float GetRemaining(BossSkill skill)
    {
        if (!skill.IsValid() || !_cooldowns.TryGetValue(skill, out float remaining))
        {
            return 0.0f;
        }

        return remaining;
    }



    /// <summary>
    /// すべての技能の冷却時間をリセット（クリア）します。
    /// </summary>
    public void Reset()
    {
        _cooldowns.Clear();
    }
}
