using System.Diagnostics.CodeAnalysis;

namespace AnimalBattle.Skills;

/// <summary>
/// 全技能設定を保持し、高速な識別子引き当てを提供する統合技能ライブラリコンテナ。
/// </summary>
public class SkillLibraryConfig
{
    private readonly Dictionary<StringId, SkillConfig> _skillsById = new();

    /// <summary>TOML の各テーブルからバインドされた技能マップ</summary>
    public Dictionary<string, SkillConfig> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 技能識別タグに対応する設定を取得します。
    /// </summary>
    public bool TryGetSkill(StringId skillId, [NotNullWhen(true)] out SkillConfig? config)
        => _skillsById.TryGetValue(skillId, out config);

    /// <summary>
    /// バインド済み技能マップから高速引き当て用辞書を構築し、各技能の後処理を実行します。
    /// </summary>
    public void PostProcess()
    {
        _skillsById.Clear();
        foreach (var (key, value) in Skills)
        {
            if (string.IsNullOrWhiteSpace(value.SkillName))
            {
                value.SkillName = key;
            }
            value.PostProcess();
            _skillsById[new StringId(key)] = value;
        }
    }
}
