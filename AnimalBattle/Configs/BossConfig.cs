using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class PhaseFsmConfig
{
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; } = 1.0f;

    /// <summary>攻撃前の巡航遊弋待機時間（秒）</summary>
    public float HoverDuration { get; set; } = 1.0f;

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

    /// <summary>このフェーズで実行される技能名リスト</summary>
    public List<string> Skills { get; set; } = [];
}

/// <summary>
/// Config/boss.toml から読み込まれる Boss の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class BossConfig
{
    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; } = 90.0f;

    /// <summary>Boss の基準出現 X 座標</summary>
    public float BaseX { get; set; } = 640.0f;

    /// <summary>Boss の基準出現 Y 座標</summary>
    public float BaseY { get; set; } = 200.0f;

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; } = 2.8f;

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; } = 2.8f;

    /// <summary>Boss 銃弾被弾当たり判定幅</summary>
    public float HitboxWidth { get; set; } = 120.0f;

    /// <summary>Boss 銃弾被弾当たり判定高さ</summary>
    public float HitboxHeight { get; set; } = 100.0f;

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; } = 0.08f;

    /// <summary>Phase 1 状態機設定</summary>
    public PhaseFsmConfig Phase1 { get; set; } = new()
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
        Skills = ["Aimed3Shot", "DiveRam", "RingBurst"]
    };

    /// <summary>Phase 2 状態機設定</summary>
    public PhaseFsmConfig Phase2 { get; set; } = new()
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
        Skills = ["DoubleSpiral", "DiveRam", "RainDrop"]
    };

    /// <summary>Phase 3 状態機設定</summary>
    public PhaseFsmConfig Phase3 { get; set; } = new()
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
        Skills = ["CrossChaos", "DiveRam", "FastSniper"]
    };

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{BossConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<BossConfig>> LoadFromFile(string path = "boss.toml")
    {
        return ConfigBase<BossConfig>.LoadFromFile(path);
    }
}
