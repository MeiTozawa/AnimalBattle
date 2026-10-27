using System.Drawing;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using ErrorOr;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 技能パターン設定の共通基底クラス。
///     タイムライン、予兆、冷却などの基本パラメータを保持し、規定の仮想実装を提供します。
/// </summary>
public abstract class SkillPatternConfigBase : IBossSkillPatternConfig, IConfigLifecycle {
    private string _skillName = string.Empty;
    private StringId _cachedSkillId;
    private Color _telegraphColor;

    /// <summary>技能表示名</summary>
    public string SkillName
    {
        get => _skillName;
        set
        {
            _skillName = value;
            _cachedSkillId = string.IsNullOrWhiteSpace(value) ? StringId.Empty : new StringId(value);
        }
    }

    /// <summary>キャッシュされた技能識別子</summary>
    public StringId SkillId => _cachedSkillId;

    /// <inheritdoc />
    public virtual ErrorOr<Success> Validate() => Result.Success;

    /// <inheritdoc />
    public virtual void PostProcess() { }

    /// <summary>前隙予兆時間秒単位</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー Hex 文字列</summary>
    [ConfigurationKeyName("TelegraphColor")]
    public string TelegraphColorHex
    {
        get => string.Empty;
        set
        {
            _telegraphColor = value.ToColor();
        }
    }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor => _telegraphColor;

    /// <summary>発動持続時間秒単位</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間秒単位</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間秒単位</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>生成弾幕が近接攻撃で破壊不能かどうかのフラグ。</summary>
    public bool IsIndestructible { get; set; }

    /// <summary>キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];

    /// <summary>出招・判定中に自身へ付与されるアーマータグ一覧例として["SuperArmor"] または ["Invincible"]</summary>
    public List<string> ArmorTags { get; set; } = [];

    /// <summary>パース済みのアーマータグフラグを取得します。</summary>
    public GameplayTag ParsedArmorTag => GameplayTagHelper.ParseTags(ArmorTags);

    /// <summary>後隙：Recovery中に自身へ付与されるタグ一覧例として["Vulnerable"]</summary>
    public List<string> RecoveryTags { get; set; } = [];

    /// <summary>パース済みの後隙タグフラグを取得します。</summary>
    public GameplayTag ParsedRecoveryTag => GameplayTagHelper.ParseTags(RecoveryTags);

    /// <inheritdoc />
    public virtual ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public virtual Action<AbilityTelegraphContext>? BuildTelegraphRenderer() => null;

    /// <inheritdoc />
    public virtual void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight) { }

    /// <inheritdoc />
    public virtual void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos) { }
}
