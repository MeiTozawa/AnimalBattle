namespace AnimalBattle.Combat.Components;

/// <summary>
///     敵ユニット（Boss・小怪）の出現直後バッファ状態コンポーネント。
///     この期間中は攻撃および接触ダメージ判定が無効化され、移動可否は設定値に従います。
/// </summary>
public struct SpawnBufferState(float duration, bool canMove, byte baseAlpha = 255)
{
    /// <summary>残りバッファ時間（秒）。</summary>
    public float RemainingTime = duration;

    /// <summary>合計バッファ持続時間（秒）。</summary>
    public float TotalDuration = duration;

    /// <summary>バッファ期間中の移動許可フラグ。</summary>
    public bool CanMove = canMove;

    /// <summary>基本アルファ値。</summary>
    public byte BaseAlpha = baseAlpha;
}
