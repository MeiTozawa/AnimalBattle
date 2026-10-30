namespace AnimalBattle.Features.Tutorial.Components;

/// <summary>
///     チュートリアル中に触れて回収する目標物（コイン／リング）を表すコンポーネント。
/// </summary>
public struct TutorialTarget
{
    /// <summary>目標物の回収判定半径（ピクセル）。</summary>
    public float Radius;

    /// <summary>既に回収されたかどうか。</summary>
    public bool IsCollected;

    /// <summary>所属するフェーズ種別。</summary>
    public TutorialPhase Phase;

    /// <summary>浮遊アニメーション用の位相オフセット。</summary>
    public float FloatPhase;
}
