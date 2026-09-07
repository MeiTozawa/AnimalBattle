using System;
using System.Collections.Generic;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Boss 1 (Ancient Cat Overlord) 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class Boss1PhaseFsmConfig
{
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; } = 1.0f;

    /// <summary>攻撃前の巡航遊弋待機時間（秒）</summary>
    public float HoverDuration { get; set; } = 1.0f;

    /// <summary>このフェーズでの巡回移動時間（秒）</summary>
    public float PatrolDuration { get; set; } = 2.0f;

    /// <summary>浮遊基準 Y 座標</summary>
    public float HoverBaseY { get; set; } = 220.0f;

    /// <summary>水平浮遊速度</summary>
    public float HoverSpeedX { get; set; } = 1.5f;

    /// <summary>水平浮遊振幅</summary>
    public float HoverAmpX { get; set; } = 220.0f;

    /// <summary>垂直浮遊速度</summary>
    public float HoverSpeedY { get; set; } = 1.8f;

    /// <summary>垂直浮遊振幅</summary>
    public float HoverAmpY { get; set; } = 60.0f;

    /// <summary>浮動追従の補間速度（Lerp Speed）</summary>
    public float HoverLerpSpeed { get; set; } = 3.5f;

    /// <summary>プレイヤー X 座標への接近追従重み（0.0 ~ 1.0）</summary>
    public float PlayerPursuitWeight { get; set; } = 0.12f;

    /// <summary>突進予兆時の機体振動振幅（ピクセル）</summary>
    public float TelegraphShakeIntensity { get; set; } = 8.0f;

    /// <summary>突進着地硬直後の高空帰還速度</summary>
    public float RecoverFlybackSpeed { get; set; } = 4.0f;

    /// <summary>着地硬直から帰還を開始する時間比率（0.0 ~ 1.0）</summary>
    public float RecoverDelayRatio { get; set; } = 0.5f;

    /// <summary>フェーズ突入時の無敵・清屏演出時間（秒）</summary>
    public float TransitionDuration { get; set; } = 1.0f;

    /// <summary>フェーズ突入時の高空帰還・浮遊移動速度</summary>
    public float TransitionFloatSpeed { get; set; } = 3.0f;

    /// <summary>このフェーズで実行される技能名リスト</summary>
    public List<string> Skills { get; set; } = [];

    /// <summary>このフェーズで評価される技能意思決定ルールリスト</summary>
    public List<AttackRuleConfig> Rules { get; set; } = [];
}

/// <summary>
/// Config/boss1.toml から読み込まれる Boss 1 の基本パラメータおよび各フェーズの FSM 設定クラス。
/// 形状種別、寸法、および局所オフセット (HurtboxOffsetX/Y, BodyHitboxOffsetX/Y) を完全管理します。
/// </summary>
public sealed class Boss1Config
{
    /// <summary>Boss 表示名</summary>
    public string BossName { get; set; } = "ANCIENT CAT OVERLORD";

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; } = 60.0f;

    /// <summary>Boss の基準出現 X 座標</summary>
    public float BaseX { get; set; } = 640.0f;

    /// <summary>Boss の基準出現 Y 座標</summary>
    public float BaseY { get; set; } = 200.0f;

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; } = 0.6f;

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; } = 0.6f;

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; } = "Circle";

    /// <summary>Boss 被弾当たり判定幅（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxWidth { get; set; } = 120.0f;

    /// <summary>Boss 被弾当たり判定高さ（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxHeight { get; set; } = 100.0f;

    /// <summary>Boss 被弾当たり判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; } = 60.0f;

    /// <summary>Boss 被弾判定の X 軸局所オフセット</summary>
    public float HurtboxOffsetX { get; set; } = 0.0f;

    /// <summary>Boss 被弾判定の Y 軸局所オフセット</summary>
    public float HurtboxOffsetY { get; set; } = 0.0f;

    /// <summary>Boss 身体接触・突進攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; } = "Circle";

    /// <summary>Boss 接触攻撃判定幅（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxWidth { get; set; } = 120.0f;

    /// <summary>Boss 接触攻撃判定高さ（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxHeight { get; set; } = 100.0f;

    /// <summary>Boss 接触攻撃判定円半径（HitboxShape == "Circle" 時に使用）</summary>
    public float BodyHitboxRadius { get; set; } = 50.0f;

    /// <summary>Boss 接触判定の X 軸局所オフセット</summary>
    public float BodyHitboxOffsetX { get; set; } = 0.0f;

    /// <summary>Boss 接触判定の Y 軸局所オフセット</summary>
    public float BodyHitboxOffsetY { get; set; } = 0.0f;

    /// <summary>Boss 身体接触・突進命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; } = 1.0f;

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; } = 0.08f;

    /// <summary>Phase 1 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase1 { get; set; } = new()
    {
        HpThreshold = 1.0f,
        HoverDuration = 0.8f,
        HoverBaseY = 220.0f,
        HoverSpeedX = 1.6f,
        HoverAmpX = 240.0f,
        HoverSpeedY = 1.8f,
        HoverAmpY = 60.0f,
        HoverLerpSpeed = 3.5f,
        PlayerPursuitWeight = 0.12f,
        TelegraphShakeIntensity = 8.0f,
        RecoverFlybackSpeed = 4.0f,
        RecoverDelayRatio = 0.5f,
        TransitionDuration = 1.0f,
        TransitionFloatSpeed = 3.0f,
        Skills = ["Aimed3Shot", "DiveRam", "RingBurst"]
    };

    /// <summary>Phase 2 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase2 { get; set; } = new()
    {
        HpThreshold = 0.66f,
        HoverDuration = 0.6f,
        HoverBaseY = 250.0f,
        HoverSpeedX = 2.0f,
        HoverAmpX = 350.0f,
        HoverSpeedY = 2.4f,
        HoverAmpY = 90.0f,
        HoverLerpSpeed = 4.0f,
        PlayerPursuitWeight = 0.15f,
        TelegraphShakeIntensity = 10.0f,
        RecoverFlybackSpeed = 4.5f,
        RecoverDelayRatio = 0.45f,
        TransitionDuration = 1.0f,
        TransitionFloatSpeed = 3.5f,
        Skills = ["DoubleSpiral", "DiveRam", "RainDrop"]
    };

    /// <summary>Phase 3 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase3 { get; set; } = new()
    {
        HpThreshold = 0.33f,
        HoverDuration = 0.4f,
        HoverBaseY = 280.0f,
        HoverSpeedX = 2.6f,
        HoverAmpX = 420.0f,
        HoverSpeedY = 3.0f,
        HoverAmpY = 120.0f,
        HoverLerpSpeed = 4.5f,
        PlayerPursuitWeight = 0.20f,
        TelegraphShakeIntensity = 12.0f,
        RecoverFlybackSpeed = 5.0f,
        RecoverDelayRatio = 0.40f,
        TransitionDuration = 1.0f,
        TransitionFloatSpeed = 4.0f,
        Skills = ["CrossChaos", "DiveRam", "FastSniper"]
    };

    /// <summary>
    /// 設定に基づいた Boss 1 被弾判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight, HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    /// 設定に基づいた Boss 1 接触・突進攻撃判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された接触攻撃衝突形状バリアント</returns>
    public CollisionShape GetHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HitboxShape, BodyHitboxRadius, BodyHitboxWidth, BodyHitboxHeight, BodyHitboxOffsetX, BodyHitboxOffsetY);
    }

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{Boss1Config}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr.ErrorOr<ConfigBase<Boss1Config>> LoadFromFile(string path = ResourcePath.Config.Boss1)
    {
        return ConfigBase<Boss1Config>.LoadFromFile(path);
    }
}
