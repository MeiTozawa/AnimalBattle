using ErrorOr;

namespace AnimalBattle.Skills;

/// <summary>
/// 単一技能の全般パラメータおよび原語ブロックを保持する汎用設定クラス。
/// </summary>
public sealed class SkillConfig : IConfigLifecycle
{
    private string _skillName = string.Empty;
    private StringId _skillId = StringId.Empty;

    /// <summary>技能表示名</summary>
    public string SkillName
    {
        get => _skillName;
        set
        {
            _skillName = value;
            _skillId = string.IsNullOrWhiteSpace(value) ? StringId.Empty : new StringId(value);
        }
    }

    /// <summary>キャッシュ済み技能識別子</summary>
    public StringId SkillId => _skillId;

    /// <summary>前隙予兆時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>発動持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本クールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>有効射程距離（px）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>出招中に自身へ付与されるアーマータグ一覧（例: ["SuperArmor"]）</summary>
    public List<string> ArmorTags { get; set; } = [];

    /// <summary>パース済みアーマータグフラグ</summary>
    public GameplayTag ParsedArmorTag => GameplayTagHelper.ParseTags(ArmorTags);

    /// <summary>後隙中に自身へ付与されるタグ一覧（例: ["Vulnerable"]）</summary>
    public List<string> RecoveryTags { get; set; } = [];

    /// <summary>パース済み後隙タグフラグ</summary>
    public GameplayTag ParsedRecoveryTag => GameplayTagHelper.ParseTags(RecoveryTags);

    /// <summary>予兆演出パラメータブロック</summary>
    public TelegraphConfig Telegraph { get; set; } = new();

    /// <summary>固有位移パラメータブロック</summary>
    public SkillMotionConfig Motion { get; set; } = new();

    /// <summary>タイムライン効果ステップ一覧</summary>
    public List<SkillEffectConfig> Effects { get; set; } = [];

    /// <inheritdoc />
    public ErrorOr<Success> Validate()
    {
        if (CastDuration <= 0.0f || WindUpTime < 0.0f || RecoveryTime < 0.0f)
        {
            return Error.Validation("Skill.InvalidDuration", $"技能 '{SkillName}' の時間設定が不正です。");
        }
        return Result.Success;
    }

    /// <inheritdoc />
    public void PostProcess() { }
}
