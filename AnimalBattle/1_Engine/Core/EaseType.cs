namespace AnimalBattle.Core;

/// <summary>
///     イージング関数の計算タイプを表す列挙型。
/// </summary>
public enum EaseType {
    /// <summary>線形補間（等速）</summary>
    Linear = 0,

    /// <summary>Sine イージング（加速）</summary>
    InSine = 1,

    /// <summary>Sine イージング（減速）</summary>
    OutSine = 2,

    /// <summary>Sine イージング（加速→減速）</summary>
    InOutSine = 3,

    /// <summary>Quadratic イージング（加速）</summary>
    InQuad = 4,

    /// <summary>Quadratic イージング（減速）</summary>
    OutQuad = 5,

    /// <summary>Quadratic イージング（加速→減速）</summary>
    InOutQuad = 6,

    /// <summary>Cubic イージング（加速）</summary>
    InCubic = 7,

    /// <summary>Cubic イージング（減速）</summary>
    OutCubic = 8,

    /// <summary>Cubic イージング（加速→減速）</summary>
    InOutCubic = 9,

    /// <summary>Exponential イージング（加速）</summary>
    InExpo = 10,

    /// <summary>Exponential イージング（減速）</summary>
    OutExpo = 11,

    /// <summary>Exponential イージング（加速→減速）</summary>
    InOutExpo = 12,

    /// <summary>Elastic イージング（加速）</summary>
    InElastic = 13,

    /// <summary>Elastic イージング（減速）</summary>
    OutElastic = 14,

    /// <summary>Elastic イージング（加速→減速）</summary>
    InOutElastic = 15,

    /// <summary>Bounce イージング（加速）</summary>
    InBounce = 16,

    /// <summary>Bounce イージング（減速）</summary>
    OutBounce = 17,

    /// <summary>Bounce イージング（加速→減速）</summary>
    InOutBounce = 18
}