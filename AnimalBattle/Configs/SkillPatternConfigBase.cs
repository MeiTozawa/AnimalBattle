using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Core;
using ErrorOr;
using Flecs.NET.Core;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 技能パターン設定の共通基底クラス。
///     時序、予兆、冷却などの基本パラメータを保持し、規定の仮想実装を提供します。
/// </summary>
public abstract class SkillPatternConfigBase : IBossSkillPatternConfig {
    /// <summary>技能表示名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];

    /// <inheritdoc />
    public virtual ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events);

    /// <inheritdoc />
    public virtual Action<AbilityTelegraphContext>? BuildTelegraphRenderer() => null;

    /// <inheritdoc />
    public virtual void EmitBarrage(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight) { }

    /// <inheritdoc />
    public virtual void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos) { }
}
