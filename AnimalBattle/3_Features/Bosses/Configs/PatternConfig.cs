using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Combat;
using AnimalBattle.Core;
using AnimalBattle.Render;
using ErrorOr;
using Flecs.NET.Core;

namespace AnimalBattle.Configs;

/// <summary>
///     TOML で定義される単一キーフレームイベント設定クラス。
/// </summary>
public sealed class SkillEventConfig {
    /// <summary>発動遅延時間（秒）</summary>
    public float Time { get; set; }

    /// <summary>イベント種別名</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>パラメータ 0</summary>
    public float Param0 { get; set; }

    /// <summary>パラメータ 1</summary>
    public float Param1 { get; set; }

    /// <summary>パラメータ 2</summary>
    public float Param2 { get; set; }

    /// <summary>文字列パラメータ</summary>
    public string StrParam { get; set; } = string.Empty;
}

/// <summary>
///     俯衝突進・メテオダイブ技能の設定クラス。
/// </summary>
public sealed class DiveRamConfig : SkillPatternConfigBase {
    /// <summary>突進速度倍率</summary>
    public float DashSpeed { get; set; }

    /// <summary>着地衝撃波半径</summary>
    public float ImpactRadius { get; set; }

    /// <summary>着地炸裂弾数</summary>
    public int ImpactBulletCount { get; set; }

    /// <summary>着地炸裂弾速度</summary>
    public float ImpactBulletSpeed { get; set; }

    /// <summary>着地炸裂弾半径</summary>
    public float ImpactBulletRadius { get; set; }

    /// <summary>着地炸裂弾塗りつぶしカラー</summary>
    public Color ImpactFillColor { get; set; }

    /// <summary>着地炸裂弾外枠カラー</summary>
    public Color ImpactOutlineColor { get; set; }

    /// <summary>着地炸裂弾の発生 Y オフセット (px)</summary>
    public float ImpactOffsetY { get; set; }

    /// <summary>着地炸裂弾の特殊弾種別（Magenta, Cyan, Yellow, Black, Normal）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>突進残影トレイルカラー</summary>
    public Color TrailColor { get; set; }

    /// <summary>突進補間に使用するイージング種別</summary>
    public EaseType EaseType { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawDiveRamTelegraph(ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY, ImpactRadius, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
        => BulletEmitter.EmitDiveImpact(world, this, origin);
}

/// <summary>
///     自機狙い 3 連射技能の設定クラス。
/// </summary>
public sealed class Aimed3ShotConfig : BulletPatternConfigBase {
    /// <summary>拡散角度（ラジアン）</summary>
    public float SpreadAngle { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, BulletCount, SpreadAngle, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawAimingLasers(ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY, SpreadAngle, BulletCount, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitAimedSpread(world, this, origin, target);
        PublishBulletFired();
    }
}

/// <summary>
///     16 方向全方位リング弾幕技能の設定クラス。
/// </summary>
public sealed class RingBurstConfig : BulletPatternConfigBase {
    /// <summary>弾間の角度間隔（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, BulletCount, AngleStep, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawRadialThreatSpikes(ctx.BossX, ctx.BossY, BulletCount, 0.0f, 75.0f, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitRadialRing(world, this, origin, ref angleOffset);
        PublishBulletFired();
    }
}

/// <summary>
///     双螺旋回転弾幕技能の設定クラス。
/// </summary>
public sealed class DoubleSpiralConfig : BulletPatternConfigBase {
    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <summary>第 1 弾の塗りつぶしカラー</summary>
    public Color FillColor1 { get; set; }

    /// <summary>第 1 弾の外枠カラー</summary>
    public Color OutlineColor1 { get; set; }

    /// <summary>第 2 弾の塗りつぶしカラー</summary>
    public Color FillColor2 { get; set; }

    /// <summary>第 2 弾の外枠カラー</summary>
    public Color OutlineColor2 { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, 2, AngleStep, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => {
            var spiralAngle = ctx.StateTimer * 4.0f;
            TelegraphRenderHelper.DrawRadialThreatSpikes(ctx.BossX, ctx.BossY, 2, spiralAngle, 90.0f, ctx.Progress, TelegraphColor);
        };

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitDoubleSpiral(world, this, origin, ref angleOffset);
        PublishBulletFired();
    }
}

/// <summary>
///     降雨弾幕技能の設定クラス。
/// </summary>
public sealed class RainDropConfig : BulletPatternConfigBase {
    /// <summary>予兆柱の表示本数</summary>
    public int TelegraphColumnCount { get; set; } = 6;

    /// <summary>予兆柱の高さ</summary>
    public float TelegraphColumnHeight { get; set; } = 650.0f;

    /// <summary>予兆警告テキスト</summary>
    public string TelegraphWarningText { get; set; } = "▼ RAIN ▼";

    /// <summary>弾生成の最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>弾生成の最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, 1, 0.0f, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawSkyfallColumns(MinX, MaxX, TelegraphColumnCount, TelegraphColumnHeight, ctx.Progress, TelegraphColor, TelegraphWarningText);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitRainDrop(world, this);
        PublishBulletFired();
    }
}

/// <summary>
///     全方位交差波弾幕技能の設定クラス。
/// </summary>
public sealed class CrossChaosConfig : BulletPatternConfigBase {
    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, 4, AngleStep, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => {
            var crossAngle = ctx.StateTimer * 2.5f;
            TelegraphRenderHelper.DrawRadialThreatSpikes(ctx.BossX, ctx.BossY, 4, crossAngle, 110.0f, ctx.Progress, TelegraphColor);
        };

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitCrossChaos(world, this, origin, ref angleOffset);
        PublishBulletFired();
    }
}

/// <summary>
///     高速レーザースナイパー弾幕技能の設定クラス。
/// </summary>
public sealed class FastSniperConfig : BulletPatternConfigBase {
    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, EffectiveRange, Events, Interval, BulletSpeed, 1, 0.0f, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawAimingLasers(ctx.BossX, ctx.BossY, ctx.TargetX, ctx.TargetY, 0.0f, 1, ctx.Progress, TelegraphColor, true);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
    {
        BulletEmitter.EmitSniper(world, this, origin, target);
        PublishBulletFired();
    }
}

/// <summary>
///     Boss 2 重劈斬撃技能の設定クラス。
/// </summary>
public sealed class HeavySlashConfig : MeleePatternConfigBase {
    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, SlashRangeX, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawMeleeDangerBox((int)ctx.BossX, (int)ctx.BossY, SlashRangeX, SlashRangeY, ctx.FacingRight, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos)
    {
        var b2Cfg = ConfigManager.Boss2;
        var offX = offsetX != 0.0f ? offsetX : forwardDir * b2Cfg.SlashSpawnOffsetX;
        var offY = offsetY != 0.0f ? offsetY : b2Cfg.SlashSpawnOffsetY;
        var dir = forwardDir > 0.0f ? SlashDirection.Right : SlashDirection.Left;
        MeleeSpawner.SpawnMeleeSlashHitbox(
            world,
            origin.X + offX,
            origin.Y + offY,
            vx,
            0.0f,
            SlashRangeX,
            SlashRangeY,
            SlashDamage,
            HitboxDuration,
            SlashColor,
            dir);
    }
}

/// <summary>
///     Boss 2 撼地踏撃技能の設定クラス。
/// </summary>
public sealed class GroundStompConfig : SkillPatternConfigBase {
    /// <summary>判定生成オフセット Y</summary>
    public float HitboxOffsetY { get; set; }

    /// <summary>着地判定オフセット Y</summary>
    public float GroundOffsetY { get; set; }

    /// <summary>衝撃波生成オフセット X</summary>
    public float ShockwaveOffsetX { get; set; }

    /// <summary>衝撃波速度</summary>
    public float ShockwaveSpeed { get; set; }

    /// <summary>衝撃波弾半径</summary>
    public float ShockwaveRadius { get; set; }

    /// <summary>衝撃波ダメージ</summary>
    public float ShockwaveDamage { get; set; }

    /// <summary>衝撃波塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>衝撃波外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, ShockwaveSpeed, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => {
            var b2Cfg = ConfigManager.Boss2;
            var groundY = ctx.BossY + b2Cfg.CollisionHeight / 2.0f;
            TelegraphRenderHelper.DrawGroundShockwaveRails((int)ctx.BossX, groundY, ctx.Progress, TelegraphColor);
        };

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
        => BulletEmitter.EmitGroundStompShockwaves(world, this, origin, bodyHeight);

    /// <inheritdoc />
    public override void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos)
        => BulletEmitter.EmitGroundStompShockwaves(world, this, origin, bodyHeight);
}

/// <summary>
///     Boss 2 泰山圧頂技能の設定クラス。
/// </summary>
public sealed class LeapSmashConfig : SkillPatternConfigBase {
    /// <summary>跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>プレイヤー頭上への飛翔所要時間</summary>
    public float FlyDuration { get; set; }

    /// <summary>最高点ホバリング滞空時の Y 座標高度</summary>
    public float PeakAltitudeY { get; set; }

    /// <summary>最高点でのホバリング滞空時間</summary>
    public float ApexHangDuration { get; set; }

    /// <summary>垂直急降下叩きつけ速度</summary>
    public float DescentSpeed { get; set; }

    /// <summary>着地破片弾数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片飛散開始角度倍率</summary>
    public float ShrapnelAngleStartRatio { get; set; }

    /// <summary>破片飛散扇状角度幅倍率</summary>
    public float ShrapnelAngleSpanRatio { get; set; }

    /// <summary>破片生成位置の垂直 Y オフセット</summary>
    public float ShrapnelOffsetY { get; set; }

    /// <summary>破片飛散速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片弾半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー</summary>
    public Color ShrapnelOutlineColor { get; set; }

    /// <summary>着地叩きつけ近接判定半径</summary>
    public float SlashRadius { get; set; }

    /// <summary>着地叩きつけダメージ</summary>
    public float SlashDamage { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, JumpForce, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawLeapSmashTelegraph(ctx.TargetX, ctx.TargetY, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
        => BulletEmitter.EmitLeapSmashShrapnel(world, this, origin, bodyHeight);

    /// <inheritdoc />
    public override void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos)
    {
        var groundY = origin.Y + bodyHeight / 2.0f;
        MeleeSpawner.SpawnMeleeSlashHitbox(
            world,
            origin.X,
            groundY - SlashRadius * 0.5f,
            0.0f,
            0.0f,
            SlashRadius * 2.0f,
            SlashRadius * 1.5f,
            SlashDamage,
            0.25f,
            ShrapnelFillColor,
            SlashDirection.Down);

        BulletEmitter.EmitLeapSmashShrapnel(world, this, origin, bodyHeight);
    }
}

/// <summary>
///     Boss 2 二連重斬技能の設定クラス。
/// </summary>
public sealed class DoubleCleaveConfig : MeleePatternConfigBase {
    /// <summary>2 回目の斬撃追加前方オフセット X</summary>
    public float SecondSlashOffsetX { get; set; }

    /// <summary>2 回目の斬撃までのインターバル（秒）</summary>
    public float SlashInterval { get; set; }

    /// <summary>前兆危険矩形 X 範囲</summary>
    public float TelegraphRangeX { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, SlashRangeX, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawMeleeDangerBox((int)ctx.BossX, (int)ctx.BossY, TelegraphRangeX, SlashRangeY, ctx.FacingRight, ctx.Progress, TelegraphColor);

    /// <inheritdoc />
    public override void SpawnHitbox(World world, Vec2f origin, float forwardDir, float vx, float bodyHeight, float offsetX, float offsetY, Vec2f targetPos)
    {
        var b2Cfg = ConfigManager.Boss2;
        var additionalX = MathF.Abs(offsetX);
        var offX = forwardDir * (b2Cfg.SlashSpawnOffsetX + additionalX);
        var offY = offsetY != 0.0f ? offsetY : b2Cfg.SlashSpawnOffsetY;
        var dir = forwardDir > 0.0f ? SlashDirection.Right : SlashDirection.Left;
        MeleeSpawner.SpawnMeleeSlashHitbox(
            world,
            origin.X + offX,
            origin.Y + offY,
            vx,
            0.0f,
            SlashRangeX,
            SlashRangeY,
            SlashDamage,
            HitboxDuration > 0.0f ? HitboxDuration : 0.24f,
            SlashColor,
            dir);
    }
}

/// <summary>
///     Boss 2 狂暴横衝直撞技能の設定クラス。
/// </summary>
public sealed class FrenzyChargeConfig : SkillPatternConfigBase {
    /// <summary>全画面高速突進速度</summary>
    public float ChargeSpeed { get; set; }

    /// <summary>突進攻撃判定幅</summary>
    public float HitboxWidth { get; set; }

    /// <summary>突進攻撃判定高さ</summary>
    public float HitboxHeight { get; set; }

    /// <summary>突進攻撃判定持続時間（秒）</summary>
    public float HitboxDuration { get; set; }

    /// <summary>突進攻撃判定エフェクトカラー</summary>
    public Color HitboxColor { get; set; }

    /// <summary>壁激突時の破片数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー</summary>
    public Color ShrapnelOutlineColor { get; set; }

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, ChargeSpeed, Events, armorTag: ParsedArmorTag, recoveryTag: ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawChargeDangerTrack((int)ctx.BossX, (int)ctx.BossY, ConfigManager.Boss2.CollisionHeight, ctx.FacingRight, ctx.Progress, TelegraphColor);
}

/// <summary>
///     Boss 2 滅地狂吼技能の設定クラス。
/// </summary>
public sealed class EarthquakeRoarConfig : SkillPatternConfigBase {
    /// <summary>予兆柱の表示本数</summary>
    public int TelegraphColumnCount { get; set; }

    /// <summary>予兆柱の高さ</summary>
    public float TelegraphColumnHeight { get; set; }

    /// <summary>予兆警告テキスト</summary>
    public string TelegraphWarningText { get; set; } = string.Empty;

    /// <summary>咆哮波状落石の投下間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>1 回あたりの落石投下数</summary>
    public int RockCount { get; set; }

    /// <summary>落石発生最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>落石発生最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>落石落下速度</summary>
    public float RockSpeed { get; set; }

    /// <summary>落石半径</summary>
    public float RockRadius { get; set; }

    /// <summary>落石塗りつぶしカラー</summary>
    public Color RockFillColor { get; set; }

    /// <summary>落石外枠カラー</summary>
    public Color RockOutlineColor { get; set; }

    /// <summary>落石の特殊弾種別（Magenta, Cyan, Yellow, Black, Normal）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <inheritdoc />
    public override ErrorOr<AttackDefinition> BuildAttackDefinition(StringId skillTag)
        => SkillConfigLoader.BuildAttackDefinition(skillTag, WindUpTime, CastDuration, RecoveryTime, MaxX, Events, Interval, RockSpeed, RockCount > 0 ? RockCount : 1, 0.0f, ParsedArmorTag, ParsedRecoveryTag);

    /// <inheritdoc />
    public override Action<AbilityTelegraphContext>? BuildTelegraphRenderer()
        => ctx => TelegraphRenderHelper.DrawSkyfallColumns(MinX, MaxX, TelegraphColumnCount, TelegraphColumnHeight, ctx.Progress, TelegraphColor, TelegraphWarningText);

    /// <inheritdoc />
    public override void EmitBullet(World world, Vec2f origin, Vec2f target, ref float angleOffset, float forwardDir, float bodyHeight)
        => BulletEmitter.EmitEarthquakeRocks(world, this);
}

/// <summary>
///     特殊弾変調パラメータ設定クラス。
/// </summary>
public sealed class SpecialBulletConfig {
    /// <summary>青弾および黒弾の法線正弦波揺動振幅</summary>
    public float CyanWaveAmplitude { get; set; }

    /// <summary>青弾および黒弾の法線正弦波揺動周波数</summary>
    public float CyanWaveFrequency { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動振幅</summary>
    public float MagentaPulseAmplitude { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動周波数</summary>
    public float MagentaPulseFrequency { get; set; }

    /// <summary>黄弾公転衛星の回転角速度（ラジアン/秒）</summary>
    public float YellowOrbitSpeed { get; set; }

    /// <summary>黄弾公転衛星の軌道半径倍率</summary>
    public float YellowOrbitDistanceMultiplier { get; set; }

    /// <summary>黄弾公転衛星の半径倍率</summary>
    public float YellowSatelliteRadiusMultiplier { get; set; }

    /// <summary>黄弾公転衛星の接触ダメージ</summary>
    public float YellowSatelliteDamage { get; set; }

    /// <summary>黄弾公転衛星の最小半径クランプ (px)</summary>
    public float YellowSatelliteMinRadius { get; set; }

    /// <summary>Boss 被弾反撃黒弾の移動速度 (px/s)</summary>
    public float CounterBulletSpeed { get; set; }

    /// <summary>Boss 被弾反撃黒弾の判定半径 (px)</summary>
    public float CounterBulletRadius { get; set; }

    /// <summary>Phase 1 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase1 { get; set; }

    /// <summary>Phase 2 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase2 { get; set; }

    /// <summary>Phase 3 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase3 { get; set; }

    /// <summary>Boss 被弾反撃黒弾が近接攻撃で破壊不能かどうかのフラグ。</summary>
    public bool CounterBulletIsIndestructible { get; set; }

    /// <summary>
    ///     指定された TOML ファイルから特殊弾変調設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<SpecialBulletConfig>> LoadFromFile(string path = ResourcePath.Config.SpecialBullets)
        => ConfigBase<SpecialBulletConfig>.LoadFromFile(path);
}

/// <summary>
///     Boss 1 専用の技能・弾パターン設定クラス。
/// </summary>
public sealed class Boss1PatternConfig : BossPatternConfigContainerBase {
    /// <summary>俯衝突進・メテオダイブ技能設定</summary>
    public DiveRamConfig DiveRam { get; set; } = new();

    /// <summary>自機狙い 3 連射技能設定</summary>
    public Aimed3ShotConfig Aimed3Shot { get; set; } = new();

    /// <summary>16 方向全方位リング弾幕技能設定</summary>
    public RingBurstConfig RingBurst { get; set; } = new();

    /// <summary>双螺旋回転弾幕技能設定</summary>
    public DoubleSpiralConfig DoubleSpiral { get; set; } = new();

    /// <summary>降雨弾幕技能設定</summary>
    public RainDropConfig RainDrop { get; set; } = new();

    /// <summary>全方位交差波弾幕技能設定</summary>
    public CrossChaosConfig CrossChaos { get; set; } = new();

    /// <summary>高速レーザースナイパー弾幕技能設定</summary>
    public FastSniperConfig FastSniper { get; set; } = new();

    /// <summary>
    ///     指定された TOML ファイルから Boss 1 パターン設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<Boss1PatternConfig>> LoadFromFile(string path = ResourcePath.Config.Boss1Patterns)
        => ConfigBase<Boss1PatternConfig>.LoadFromFile(path);
}

/// <summary>
///     Boss 2 専用の近接・広域技能パターン設定クラス。
/// </summary>
public sealed class Boss2PatternConfig : BossPatternConfigContainerBase {
    /// <summary>Boss 2 重劈斬撃技能設定</summary>
    public HeavySlashConfig HeavySlash { get; set; } = new();

    /// <summary>Boss 2 撼地踏撃技能設定</summary>
    public GroundStompConfig GroundStomp { get; set; } = new();

    /// <summary>Boss 2 泰山圧頂技能設定</summary>
    public LeapSmashConfig LeapSmash { get; set; } = new();

    /// <summary>Boss 2 二連重斬技能設定</summary>
    public DoubleCleaveConfig DoubleCleave { get; set; } = new();

    /// <summary>Boss 2 狂暴横衝直撞技能設定</summary>
    public FrenzyChargeConfig FrenzyCharge { get; set; } = new();

    /// <summary>Boss 2 滅地狂吼技能設定</summary>
    public EarthquakeRoarConfig EarthquakeRoar { get; set; } = new();

    /// <summary>
    ///     指定された TOML ファイルから Boss 2 パターン設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<Boss2PatternConfig>> LoadFromFile(string path = ResourcePath.Config.Boss2Patterns)
        => ConfigBase<Boss2PatternConfig>.LoadFromFile(path);
}