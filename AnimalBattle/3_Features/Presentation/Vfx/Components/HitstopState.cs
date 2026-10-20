namespace AnimalBattle.Components;

/// <summary>
///     ヒットストップ（時間停止・硬直）状態コンポーネント。
///     持続時間中、エンティティの物理移動およびアニメーション更新を凍結します。
/// </summary>
public struct HitstopState
{
    /// <summary>残りヒットストップ時間（秒）。</summary>
    public float RemainingSeconds;

    public HitstopState(float duration)
    {
        RemainingSeconds = duration;
    }
}
