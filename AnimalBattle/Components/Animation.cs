namespace AnimalBattle.Components;

/// <summary>
/// アニメーション再生データを保持する純粋コンポーネント。
/// </summary>
public struct Animation
{
    /// <summary>再生中のアニメーションクリップ名</summary>
    public string CurrentClip;

    /// <summary>現在のフレームインデックス</summary>
    public int CurrentFrameIndex;

    /// <summary>アニメーション再生タイマー（秒）</summary>
    public float Timer;

    /// <summary>再生状態フラグ</summary>
    public bool IsPlaying;

    /// <summary>
    /// 指定された初期クリップ名で <see cref="Animation"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Animation(string defaultClip = "Idle", bool isPlaying = true)
    {
        CurrentClip = defaultClip;
        CurrentFrameIndex = 0;
        Timer = 0.0f;
        IsPlaying = isPlaying;
    }
}
