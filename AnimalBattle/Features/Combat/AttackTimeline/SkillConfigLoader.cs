using AnimalBattle.Abilities.Attack;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Abilities.Skills;

/// <summary>
///     TOML 設定から検証済みの <see cref="AttackDefinition" /> を構築するデータローダーです。
/// </summary>
public static class SkillConfigLoader {
    /// <summary>
    ///     パラメータ群およびキーフレームイベント一覧から不変タイムライン定義をビルドします。
    /// </summary>
    /// <param name="skillTag">技能識別タグ。</param>
    /// <param name="windUpTime">前隙予兆時間。</param>
    /// <param name="castDuration">発動持続時間。</param>
    /// <param name="recoveryTime">後隙硬直時間。</param>
    /// <param name="effectiveRange">有効射程。</param>
    /// <param name="events">TOML キーフレームイベント一覧。</param>
    /// <param name="defaultInterval">周期弾幕の生成間隔。</param>
    /// <param name="defaultSpeed">周期弾幕の弾速。</param>
    /// <param name="defaultCount">周期弾幕の弾数。</param>
    /// <param name="defaultSpread">周期弾幕の拡散角度。</param>
    /// <param name="armorTag">出招中のアーマー種別。</param>
    /// <param name="recoveryTag">後隙硬直中に付与されるタグ。</param>
    /// <returns>成功時は <see cref="AttackDefinition" />、検証失敗時は <see cref="Error" />。</returns>
    public static ErrorOr<AttackDefinition> BuildAttackDefinition(
        StringId skillTag,
        float windUpTime,
        float castDuration,
        float recoveryTime,
        float effectiveRange,
        IReadOnlyList<SkillEventConfig>? events,
        float defaultInterval = 0.0f,
        float defaultSpeed = 0.0f,
        int defaultCount = 0,
        float defaultSpread = 0.0f,
        GameplayTag armorTag = GameplayTag.None,
        GameplayTag recoveryTag = GameplayTag.None)
    {
        // 業務バリデーション
        if (castDuration <= 0.0f || windUpTime < 0.0f || recoveryTime < 0.0f)
            return Error.Validation(
                "Skill.InvalidDuration",
                $"技能 '{skillTag}' の時間設定が不正です。");

        if (effectiveRange < 0.0f)
            return Error.Validation("Skill.InvalidRange", $"技能 '{skillTag}' の有効射程が負数です: {effectiveRange}");

        var actions = new List<AttackActionStep>();
        var index = 0;

        if (events != null && events.Count > 0)
            foreach (var ev in events)
            {
                var kind = ResolveActionKind(ev.Type);
                actions.Add(new AttackActionStep(kind, index++, ev.Param0, ev.Param1, ev.Param2, ev.Time));
            }
        else if (defaultInterval > 0.0f && defaultSpeed > 0.0f)
            // 旧来パラメータに基づく周期的弾幕自動生成フォールバック
            for (var t = 0.0f; t < castDuration; t += defaultInterval)
                actions.Add(new AttackActionStep(
                    AttackActionKind.Bullet,
                    index++,
                    defaultSpeed,
                    defaultCount > 0 ? defaultCount : 1,
                    defaultSpread,
                    t));

        var definition = new AttackDefinition(
            skillTag,
            windUpTime,
            castDuration,
            recoveryTime,
            effectiveRange,
            actions.ToArray(),
            armorTag,
            recoveryTag);

        if (!definition.IsValid) return Error.Validation("Skill.InvalidDefinition", $"技能 '{skillTag}' の内部定義検証に失敗しました。");

        return definition;
    }

    private static AttackActionKind ResolveActionKind(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return FallbackUnknownType("<empty>");

        return typeName.Trim().ToLowerInvariant() switch {
            "bullet" => AttackActionKind.Bullet,
            "hitbox" => AttackActionKind.Hitbox,
            "movement" => AttackActionKind.Movement,
            "easingmovement" => AttackActionKind.EasingMovement,
            "velocitydive" => AttackActionKind.VelocityDive,
            "ongroundedhitbox" => AttackActionKind.OnGroundedHitbox,
            "shake" or "screenshake" => AttackActionKind.ScreenShake,
            _ => FallbackUnknownType(typeName)
        };
    }

    private static AttackActionKind FallbackUnknownType(string typeName)
    {
        Debug.Assert(false, $"[SkillConfigLoader] 未知のイベント種別です: '{typeName}'。Bullet としてフォールバックします。");
        return AttackActionKind.Bullet;
    }
}