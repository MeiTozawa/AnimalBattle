namespace AnimalBattle.Combat.Components;

/// <summary>
///     敵ユニット（Boss・小怪）の出現直後バッファ状態コンポーネント。
///     この期間中は攻撃および接触ダメージ判定が無効化されます。
/// </summary>
public struct SpawnBufferState(float duration)
{
    /// <summary>残りバッファ時間（秒）。</summary>
    public float RemainingTime = duration;
}
