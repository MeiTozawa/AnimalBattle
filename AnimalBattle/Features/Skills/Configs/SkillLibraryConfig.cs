using System.Diagnostics.CodeAnalysis;
using ErrorOr;

namespace AnimalBattle.Skills;

/// <summary>
/// 全技能設定を保持し、高速な識別子引き当てを提供する統合技能ライブラリコンテナ。
/// </summary>
public class SkillLibraryConfig : IConfigLifecycle
{
    private Dictionary<StringId, SkillConfig>? _cache;

    /// <summary>TOML の各テーブルからバインドされた技能マップ</summary>
    public Dictionary<string, SkillConfig> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 技能識別タグに対応する設定を取得します。
    /// </summary>
    public bool TryGetSkill(StringId skillId, [NotNullWhen(true)] out SkillConfig? config)
    {
        _cache ??= BuildCache();
        return _cache.TryGetValue(skillId, out config);
    }

    /// <inheritdoc />
    public ErrorOr<Success> Validate() => Result.Success;

    /// <inheritdoc />
    public void PostProcess()
    {
        _cache = BuildCache();
        foreach (var skill in _cache.Values)
        {
            skill.PostProcess();
        }
    }

    private Dictionary<StringId, SkillConfig> BuildCache()
    {
        var cache = new Dictionary<StringId, SkillConfig>();
        foreach (var (key, value) in Skills)
        {
            if (string.IsNullOrWhiteSpace(value.SkillName))
            {
                value.SkillName = key;
            }
            cache[new StringId(key)] = value;
        }
        return cache;
    }
}
