namespace AnimalBattle.Core;

/// <summary>
/// イージング関数の計算タイプを表す列挙型。
/// </summary>
public enum EaseType
{
    /// <summary>線形補間（等速）</summary>
    Linear,

    /// <summary>Sine イージング（加速）</summary>
    InSine,

    /// <summary>Sine イージング（減速）</summary>
    OutSine,

    /// <summary>Sine イージング（加速→減速）</summary>
    InOutSine,

    /// <summary>Quadratic イージング（加速）</summary>
    InQuad,

    /// <summary>Quadratic イージング（減速）</summary>
    OutQuad,

    /// <summary>Quadratic イージング（加速→減速）</summary>
    InOutQuad,

    /// <summary>Cubic イージング（減速）</summary>
    OutCubic,

    /// <summary>Elastic イージング（減速）</summary>
    OutElastic,

    /// <summary>Bounce イージング（減速）</summary>
    OutBounce
}
