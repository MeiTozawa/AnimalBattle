using System.Diagnostics.CodeAnalysis;
using AnimalBattle.Combat;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 技能パターン設定コンテナの共通基底クラス。
///     各 Boss が保持する技能設定プロパティを検出し、StringId による高速引き当てキャッシュを提供します。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public abstract class BossPatternConfigContainerBase {
    private Dictionary<StringId, IBossSkillPatternConfig>? _skillCache;

    /// <summary>
    ///     技能識別タグに対応するパターン設定を動的に取得します。
    /// </summary>
    public bool TryGetSkill(StringId skill, [NotNullWhen(true)] out IBossSkillPatternConfig? pattern)
    {
        _skillCache ??= BuildSkillCache();
        return _skillCache.TryGetValue(skill, out pattern);
    }

    /// <summary>
    ///     文字列で指定された弾種別をパースします。
    /// </summary>
    public static BulletType ParseBulletType(string? typeStr, BulletType defaultType = BulletType.Normal)
    {
        if (!string.IsNullOrWhiteSpace(typeStr) && Enum.TryParse(typeStr, true, out BulletType result))
        {
            return result;
        }
        return defaultType;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
        Justification = "Pattern config containers have their public properties preserved.")]
    private Dictionary<StringId, IBossSkillPatternConfig> BuildSkillCache()
    {
        var cache = new Dictionary<StringId, IBossSkillPatternConfig>();
        foreach (var prop in GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (typeof(IBossSkillPatternConfig).IsAssignableFrom(prop.PropertyType) &&
                prop.GetValue(this) is IBossSkillPatternConfig skillConfig)
            {
                cache[new StringId(prop.Name)] = skillConfig;
            }
        }
        return cache;
    }
}
