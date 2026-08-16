using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Config/boss.toml から読み込まれる Boss の基本パラメータおよび各フェーズの浮遊運動設定クラス。
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

    /// <summary>フェーズ 1 の水平浮遊周期速度</summary>
    public float Phase1HoverSpeedX { get; set; } = 1.5f;

    /// <summary>フェーズ 1 の水平浮遊振幅（ピクセル）</summary>
    public float Phase1HoverAmpX { get; set; } = 80.0f;

    /// <summary>フェーズ 1 の垂直浮遊周期速度</summary>
    public float Phase1HoverSpeedY { get; set; } = 2.0f;

    /// <summary>フェーズ 1 の垂直浮遊振幅（ピクセル）</summary>
    public float Phase1HoverAmpY { get; set; } = 25.0f;

    /// <summary>フェーズ 2 の水平浮遊周期速度</summary>
    public float Phase2HoverSpeedX { get; set; } = 1.2f;

    /// <summary>フェーズ 2 の水平浮遊振幅（ピクセル）</summary>
    public float Phase2HoverAmpX { get; set; } = 260.0f;

    /// <summary>フェーズ 2 の垂直浮遊周期速度</summary>
    public float Phase2HoverSpeedY { get; set; } = 2.4f;

    /// <summary>フェーズ 2 の垂直浮遊振幅（ピクセル）</summary>
    public float Phase2HoverAmpY { get; set; } = 40.0f;

    /// <summary>フェーズ 3 の水平浮遊周期速度</summary>
    public float Phase3HoverSpeedX { get; set; } = 2.2f;

    /// <summary>フェーズ 3 の水平浮遊振幅（ピクセル）</summary>
    public float Phase3HoverAmpX { get; set; } = 320.0f;

    /// <summary>フェーズ 3 の垂直浮遊周期速度</summary>
    public float Phase3HoverSpeedY { get; set; } = 3.6f;

    /// <summary>フェーズ 3 の垂直浮遊振幅（ピクセル）</summary>
    public float Phase3HoverAmpY { get; set; } = 50.0f;

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
