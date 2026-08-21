namespace AnimalBattle.Components;

/// <summary>
/// Boss 1 (Ancient Cat Overlord) の実行時ステータスおよび座標基準データを保持するコンポーネント。
/// </summary>
public struct Boss1Stats
{
    /// <summary>現在の戦闘段階 (Phase 1, 2, 3)</summary>
    public int Phase;

    /// <summary>出現・浮遊基準 X 座標</summary>
    public float BaseX;

    /// <summary>出現・浮遊基準 Y 座標</summary>
    public float BaseY;

    /// <summary>浮遊移動の累積時間（秒）</summary>
    public float HoverTimer;

    /// <summary>被弾ホワイトフラッシュ演出タイマー（秒）</summary>
    public float HitFlashTimer;

    /// <summary>弾幕回転用累積角度オフセット（ラジアン）</summary>
    public float AngleOffset;

    /// <summary>
    /// <see cref="Boss1Stats"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="baseX">出現基準 X 座標</param>
    /// <param name="baseY">出現基準 Y 座標</param>
    public Boss1Stats(float baseX = 640.0f, float baseY = 200.0f)
    {
        Phase = 1;
        BaseX = baseX;
        BaseY = baseY;
        HoverTimer = 0.0f;
        HitFlashTimer = 0.0f;
        AngleOffset = 0.0f;
    }
}
