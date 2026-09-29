namespace AnimalBattle.Components;

/// <summary>
///     特殊弾幕（品紅・黄・青・黒）の周期アニメーション、法線揺動、拡縮制御用データを保持するコンポーネント。
/// </summary>
/// <remarks>
///     <see cref="BulletModifier" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="type">特殊種別。</param>
/// <param name="baseRadius">基準半径。</param>
/// <param name="baseVx">基準 X 速度。</param>
/// <param name="baseVy">基準 Y 速度。</param>
public struct BulletModifier(BulletType type, float baseRadius, float baseVx, float baseVy) {
    /// <summary>弾幕の特殊種別。</summary>
    public BulletType Type = type;

    /// <summary>基準となる初期半径（ピクセル）。</summary>
    public float BaseRadius = baseRadius;

    /// <summary>初期直進 X 速度。</summary>
    public float BaseVx = baseVx;

    /// <summary>初期直進 Y 速度。</summary>
    public float BaseVy = baseVy;

    /// <summary>生成からの累積生存時間タイマー（秒）。</summary>
    public float ElapsedTimer = 0.0f;
}