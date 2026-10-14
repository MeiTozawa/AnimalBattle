using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 技能パターン設定の共通インターフェース。
///     時序定義、前兆演出、弾幕および判定生成を自身に内包し、
///     コード内での技能名ハードコードおよび switch-case を完全に排除します。
/// </summary>
public interface IBossSkillPatternConfig {
    /// <summary>技能表示名</summary>
    string SkillName { get; set; }

    /// <summary>前隙予兆時間</summary>
    float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    float DefaultCooldown { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    List<SkillEventConfig> Events { get; set; }


    /// <summary>技能識別タグに対応する不変攻撃時序定義をビルドします。</summary>
    ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag);

    /// <summary>前兆演出レンダラーを生成します。</summary>
    Action<AbilityTelegraphContext>? BuildTelegraphRenderer();

    /// <summary>弾幕波次を生成します。</summary>
    void EmitBullet(Flecs.NET.Core.World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight);

    /// <summary>近接判定および派生弾幕を生成します。</summary>
    void SpawnHitbox(Flecs.NET.Core.World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos);
}
