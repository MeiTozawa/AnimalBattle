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

    /// <summary>水平浮遊速度</summary>
    public float HoverSpeedX { get; set; } = 1.5f;

    /// <summary>水平浮遊振幅</summary>
    public float HoverAmpX { get; set; } = 80.0f;

    /// <summary>垂直浮遊速度</summary>
    public float HoverSpeedY { get; set; } = 2.0f;

    /// <summary>垂直浮遊振幅</summary>
    public float HoverAmpY { get; set; } = 25.0f;

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
    public float BaseY { get; set; } = 180.0f;

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
        HoverDuration = 1.0f,
        HoverSpeedX = 1.5f,
        HoverAmpX = 80.0f,
        HoverSpeedY = 2.0f,
        HoverAmpY = 25.0f,
        TransitionDuration = 1.0f,
        Skills = ["Aimed3Shot", "RingBurst"]
    };

    /// <summary>Phase 2 状態機設定</summary>
    public PhaseFsmConfig Phase2 { get; set; } = new()
    {
        HpThreshold = 0.66f,
        HoverDuration = 0.7f,
        HoverSpeedX = 1.2f,
        HoverAmpX = 260.0f,
        HoverSpeedY = 2.4f,
        HoverAmpY = 40.0f,
        TransitionDuration = 1.0f,
        Skills = ["DoubleSpiral", "RainDrop"]
    };

    /// <summary>Phase 3 状态机设置</summary>
    public PhaseFsmConfig Phase3 { get; set; } = new()
    {
        HpThreshold = 0.33f,
        HoverDuration = 0.4f,
        HoverSpeedX = 2.2f,
        HoverAmpX = 320.0f,
        HoverSpeedY = 3.6f,
        HoverAmpY = 50.0f,
        TransitionDuration = 1.0f,
        Skills = ["CrossChaos", "FastSniper"]
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
