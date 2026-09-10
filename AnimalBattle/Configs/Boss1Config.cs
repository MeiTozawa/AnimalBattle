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
    public float HpThreshold { get; set; }

    /// <summary>攻撃前の巡航遊弋待機時間（秒）</summary>
    public float HoverDuration { get; set; }

    /// <summary>このフェーズでの巡回移動時間（秒）</summary>
    public float PatrolDuration { get; set; }

    /// <summary>浮遊基準 Y 座標</summary>
    public float HoverBaseY { get; set; }

    /// <summary>水平浮遊速度</summary>
    public float HoverSpeedX { get; set; }

    /// <summary>水平浮遊振幅</summary>
    public float HoverAmpX { get; set; }

    /// <summary>垂直浮遊速度</summary>
    public float HoverSpeedY { get; set; }

    /// <summary>垂直浮遊振幅</summary>
    public float HoverAmpY { get; set; }

    /// <summary>浮動追従の補間速度（Lerp Speed）</summary>
    public float HoverLerpSpeed { get; set; }

    /// <summary>プレイヤー X 座標への接近追従重み（0.0 ~ 1.0）</summary>
    public float PlayerPursuitWeight { get; set; }

    /// <summary>突進予兆時の機体振動振幅（ピクセル）</summary>
    public float TelegraphShakeIntensity { get; set; }

    /// <summary>突進着地硬直後の高空帰還速度</summary>
    public float RecoverFlybackSpeed { get; set; }

    /// <summary>着地硬直から帰還を開始する時間比率（0.0 ~ 1.0）</summary>
    public float RecoverDelayRatio { get; set; }

    /// <summary>フェーズ突入時の無敵・清屏演出時間（秒）</summary>
    public float TransitionDuration { get; set; }

    /// <summary>フェーズ突入時の高空帰還・浮遊移動速度</summary>
    public float TransitionFloatSpeed { get; set; }

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
    public string BossName { get; set; } = string.Empty;

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; }

    /// <summary>Boss の出現 X 座標</summary>
    public float SpawnX { get; set; } = 640.0f;

    /// <summary>Boss の出現 Y 座標</summary>
    public float SpawnY { get; set; } = 200.0f;

    /// <summary>旧 BaseX との互換アクセサ</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float BaseX
    {
        get => SpawnX;
        set => SpawnX = value;
    }

    /// <summary>旧 BaseY との互換アクセサ</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public float BaseY
    {
        get => SpawnY;
        set => SpawnY = value;
    }

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; }

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; }

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; } = string.Empty;

    /// <summary>Boss 被弾当たり判定幅（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxWidth { get; set; }

    /// <summary>Boss 被弾当たり判定高さ（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxHeight { get; set; }

    /// <summary>Boss 被弾当たり判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; }

    /// <summary>Boss 被弾判定の X 軸局所オフセット</summary>
    public float HurtboxOffsetX { get; set; }

    /// <summary>Boss 被弾判定の Y 軸局所オフセット</summary>
    public float HurtboxOffsetY { get; set; }

    /// <summary>Boss 身体接触・突進攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; } = string.Empty;

    /// <summary>Boss 接触攻撃判定幅（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxWidth { get; set; }

    /// <summary>Boss 接触攻撃判定高さ（HitboxShape == "Box" 時に使用）</summary>
    public float BodyHitboxHeight { get; set; }

    /// <summary>Boss 接触攻撃判定円半径（HitboxShape == "Circle" 時に使用）</summary>
    public float BodyHitboxRadius { get; set; }

    /// <summary>Boss 接触判定の X 軸局所オフセット</summary>
    public float BodyHitboxOffsetX { get; set; }

    /// <summary>Boss 接触判定の Y 軸局所オフセット</summary>
    public float BodyHitboxOffsetY { get; set; }

    /// <summary>Boss 身体接触・突進命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; }

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>Phase 1 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase1 { get; set; } = new();

    /// <summary>Phase 2 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase2 { get; set; } = new();

    /// <summary>Phase 3 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase3 { get; set; } = new();

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
