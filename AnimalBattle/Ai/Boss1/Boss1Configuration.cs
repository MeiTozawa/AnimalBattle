using System.Diagnostics;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の移動に必要な Phase 設定の不変 snapshot です。</summary>
/// <remarks>Boss1 Phase 設定を生成します。</remarks>
public readonly struct Boss1PhaseSettings(
    float hoverDuration,
    float hoverBaseY,
    float hoverSpeedX,
    float hoverAmpX,
    float hoverSpeedY,
    float hoverAmpY,
    float playerPursuitWeight,
    float hoverLerpSpeed,
    float telegraphShakeIntensity,
    float recoverFlybackSpeed,
    float recoverDelayRatio,
    float transitionDuration,
    float transitionFloatSpeed,
    IReadOnlyList<StringId> attackOrder) {
    /// <summary>Hover の継続時間です。</summary>
    public float HoverDuration { get; } = hoverDuration;

    /// <summary>Hover の基準 Y 座標です。</summary>
    public float HoverBaseY { get; } = hoverBaseY;

    /// <summary>水平移動の角速度です。</summary>
    public float HoverSpeedX { get; } = hoverSpeedX;

    /// <summary>水平移動の振幅です。</summary>
    public float HoverAmpX { get; } = hoverAmpX;

    /// <summary>垂直移動の角速度です。</summary>
    public float HoverSpeedY { get; } = hoverSpeedY;

    /// <summary>垂直移動の振幅です。</summary>
    public float HoverAmpY { get; } = hoverAmpY;

    /// <summary>プレイヤーへの追従重みです。</summary>
    public float PlayerPursuitWeight { get; } = playerPursuitWeight;

    /// <summary>座標追従の平滑補間速度です。</summary>
    public float HoverLerpSpeed { get; } = hoverLerpSpeed;

    /// <summary>突進予兆の振動量です。</summary>
    public float TelegraphShakeIntensity { get; } = telegraphShakeIntensity;

    /// <summary>攻撃後の帰還速度です。</summary>
    public float RecoverFlybackSpeed { get; } = recoverFlybackSpeed;

    /// <summary>帰還開始の Recovery 比率です。</summary>
    public float RecoverDelayRatio { get; } = recoverDelayRatio;

    /// <summary>Phase 遷移時間です。</summary>
    public float TransitionDuration { get; } = transitionDuration;

    /// <summary>Phase 遷移中の帰還速度です。</summary>
    public float TransitionFloatSpeed { get; } = transitionFloatSpeed;

    /// <summary>この Phase の技能順です。</summary>
    public IReadOnlyList<StringId> AttackOrder { get; } =
        attackOrder ?? throw new ArgumentNullException(nameof(attackOrder));
}

/// <summary>Boss1 攻撃の三段階と固有パラメータを不変に保持します。</summary>
/// <remarks>Boss1 攻撃 pattern を生成します。</remarks>
public sealed class Boss1AttackPattern(
    StringId skillTag,
    AttackDefinition definition,
    float interval = 0,
    float bulletSpeed = 0,
    int bulletCount = 0,
    float angleStep = 0,
    float minX = 0,
    float maxX = 0,
    float dashSpeed = 0,
    EaseType easeType = EaseType.Linear) {
    /// <summary>Boss1 攻撃 pattern を生成します。</summary>
    public Boss1AttackPattern(
        StringId skillTag,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float interval = 0,
        float bulletSpeed = 0,
        int bulletCount = 0,
        float angleStep = 0,
        float minX = 0,
        float maxX = 0,
        float dashSpeed = 0,
        EaseType easeType = EaseType.Linear)
        : this(
            skillTag,
            new AttackDefinition(skillTag, telegraphDuration, executeDuration, recoveryDuration, 0),
            interval,
            bulletSpeed,
            bulletCount,
            angleStep,
            minX,
            maxX,
            dashSpeed,
            easeType)
    {
    }

    /// <summary>安定した技能識別タグです。</summary>
    public StringId SkillTag { get; } = skillTag;

    /// <summary>共通 Telegraph/Execute/Recovery 定義です。</summary>
    public AttackDefinition Definition { get; } = definition;

    /// <summary>連射間隔です。</summary>
    public float Interval { get; } = interval;

    /// <summary>弾速です。</summary>
    public float BulletSpeed { get; } = bulletSpeed;

    /// <summary>弾数です。</summary>
    public int BulletCount { get; } = bulletCount;

    /// <summary>角度変化量または拡散角です。</summary>
    public float AngleStep { get; } = angleStep;

    /// <summary>降雨弾の最小 X 座標です。</summary>
    public float MinX { get; } = minX;

    /// <summary>降雨弾の最大 X 座標です。</summary>
    public float MaxX { get; } = maxX;

    /// <summary>突進の設定速度倍率です。</summary>
    public float DashSpeed { get; } = dashSpeed;

    /// <summary>突進の easing 種別です。</summary>
    public EaseType EaseType { get; } = easeType;

    /// <summary>三段階と固有パラメータが実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid
     && ((Interval > 0 && BulletSpeed > 0) || DashSpeed > 0 || (MaxX > MinX));
}

/// <summary>Boss1 State_Module が読む設定の注入境界です。</summary>
public interface IBoss1Configuration {
    /// <summary>急降下突進技能識別タグを取得します。</summary>
    StringId DiveSkillTag { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss1PhaseSettings GetPhase(Phase phase);

    /// <summary>指定技能タグの検証済み pattern を取得します。</summary>
    bool GetAttack(StringId skill, out Boss1AttackPattern pattern);

    /// <summary>指定技能タグのデフォルトクールダウン時間を取得します。</summary>
    float GetSkillDefaultCooldown(StringId skill);
}

/// <summary>既定値を保持する Boss1 設定です。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration {
    /// <inheritdoc />
    public StringId DiveSkillTag => new(ConfigManager.Boss1.DiveSkill);

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        try
        {
            var tomlPhase = phase switch {
                Phase.Phase1 => ConfigManager.Boss1.Phase1,
                Phase.Phase2 => ConfigManager.Boss1.Phase2,
                Phase.Phase3 => ConfigManager.Boss1.Phase3,
                _ => AssertHelper.FailWithDefault($"[Boss1Configuration 警告] 未知のフェーズ: {phase}", ConfigManager.Boss1.Phase1)
            };

            var order = tomlPhase.Skills.Count > 0
                ? tomlPhase.Skills.Select(s => new StringId(s)).Where(id => !id.IsEmpty).ToArray()
                : Array.Empty<StringId>();

            return new Boss1PhaseSettings(
                tomlPhase.HoverDuration,
                tomlPhase.HoverBaseY,
                tomlPhase.HoverSpeedX,
                tomlPhase.HoverAmpX,
                tomlPhase.HoverSpeedY,
                tomlPhase.HoverAmpY,
                tomlPhase.PlayerPursuitWeight,
                tomlPhase.HoverLerpSpeed,
                tomlPhase.TelegraphShakeIntensity,
                tomlPhase.RecoverFlybackSpeed,
                tomlPhase.RecoverDelayRatio,
                tomlPhase.TransitionDuration,
                tomlPhase.TransitionFloatSpeed,
                order);
        }
        catch (Exception ex)
        {
            Debug.Fail($"[Boss1Configuration 警告] フェーズ設定の取得中に例外が発生しました: {ex.Message}", ex.ToString());
            return new Boss1PhaseSettings(0.8f, 220.0f, 1.6f, 240.0f, 1.8f, 60.0f, 0.12f, 3.5f, 8.0f, 4.0f, 0.5f, 1.0f, 3.0f, Array.Empty<StringId>());
        }
    }

    /// <inheritdoc />
    public bool GetAttack(StringId skill, out Boss1AttackPattern pattern)
    {
        try
        {
            var toml = ConfigManager.Boss1Patterns;
            if (toml.TryGetSkill(skill, out var skillConfig) && skillConfig is IBoss1SkillPatternConfig b1Config)
            {
                pattern = b1Config.BuildLegacyAttackPattern(skill);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.Fail($"[Boss1Configuration 警告] 攻撃パターンの取得中に例外が発生しました: {ex.Message}", ex.ToString());
        }

        pattern = null!;
        return false;
    }

    /// <inheritdoc />
    public float GetSkillDefaultCooldown(StringId skill)
    {
        var toml = ConfigManager.Boss1Patterns;
        return toml.TryGetSkill(skill, out var skillConfig) ? skillConfig.DefaultCooldown : 0.0f;
    }
}