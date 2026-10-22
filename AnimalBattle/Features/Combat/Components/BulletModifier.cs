using AnimalBattle.Core;

namespace AnimalBattle.Combat.Components;

/// <summary>
///     特殊弾幕：品紅・黄・青・黒の周期アニメーション、法線揺動、拡縮制御用データを保持するコンポーネント。
/// </summary>
/// <remarks>
///     <see cref="BulletModifier" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="type">特殊種別。</param>
/// <param name="baseRadius">基準半径。</param>
/// <param name="baseVel">基準速度ベクトル Vec2f型。</param>
public struct BulletModifier(BulletType type, float baseRadius, Vec2f baseVel) {
    /// <summary>弾幕の特殊種別。</summary>
    public BulletType Type = type;

    /// <summary>基準となる初期半径ピクセル単位。</summary>
    public float BaseRadius = baseRadius;

    /// <summary>初期直進速度ベクトル Vec2f型。</summary>
    public Vec2f BaseVel = baseVel;

    /// <summary>生成からの累積生存時間タイマー秒単位。</summary>
    public float ElapsedTimer = 0.0f;
}