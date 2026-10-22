using System.Diagnostics.CodeAnalysis;
using AnimalBattle.Combat;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 技能パターン設定コンテナの共通基底クラス。
///     StringId による高速引き当てキャッシュを提供します。
/// </summary>
public abstract class BossPatternConfigContainerBase : IConfigLifecycle {
    private Dictionary<StringId, IBossSkillPatternConfig>? _skillCache;

    /// <summary>
    ///     技能識別タグに対応するパターン設定を動的に取得します。
    /// </summary>
    public bool TryGetSkill(StringId skill, [NotNullWhen(true)] out IBossSkillPatternConfig? pattern)
    {
        _skillCache ??= BuildSkillCache();
        return _skillCache.TryGetValue(skill, out pattern);
    }

    /// <inheritdoc />
    public virtual ErrorOr<Success> Validate() => Result.Success;

    /// <inheritdoc />
    public virtual void PostProcess()
    {
        _skillCache ??= BuildSkillCache();
        foreach (var skill in _skillCache.Values)
        {
            if (skill is IConfigLifecycle lifecycle)
            {
                lifecycle.PostProcess();
            }
        }
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

    /// <summary>
    ///     サブクラス固有の技能設定をキャッシュ辞書へ登録します。
    ///     AOT 環境でトリミング耐性とゼロリフレクションを保証するため、派生クラスで実装されます。
    /// </summary>
    protected abstract void PopulateSkills(Dictionary<StringId, IBossSkillPatternConfig> cache);

    private Dictionary<StringId, IBossSkillPatternConfig> BuildSkillCache()
    {
        var cache = new Dictionary<StringId, IBossSkillPatternConfig>();
        PopulateSkills(cache);
        return cache;
    }
}
