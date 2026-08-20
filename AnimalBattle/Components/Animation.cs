using System;
using AnimalBattle.Configs;

namespace AnimalBattle.Components;

/// <summary>
/// アニメーション再生状態およびキャラクター種別を保持する純粋コンポーネント。
/// </summary>
/// <remarks>
/// 指定された初期クリップ名およびキャラクター種別で <see cref="Animation"/> の新しいインスタンスを初期化します。
/// </remarks>
public struct Animation(string defaultClip = "Idle", CharacterType characterType = CharacterType.Cat, bool isPlaying = true)
{
    /// <summary>再生対象のキャラクター種別</summary>
    public CharacterType CharacterType = characterType;

    /// <summary>再生中のアニメーションクリップ名</summary>
    public string CurrentClip = defaultClip;

    /// <summary>現在のフレームインデックス</summary>
    public int CurrentFrameIndex = 0;

    /// <summary>アニメーション再生タイマー（秒）</summary>
    public float Timer = 0.0f;

    /// <summary>再生状態フラグ</summary>
    public bool IsPlaying = isPlaying;

    /// <summary>非ループ再生が完了したかどうかのフラグ</summary>
    public bool IsFinished = false;

    /// <summary>
    /// クリップを変更してアニメーションを再生します。異なるクリップが指定された場合は再生時間を 0 にリセットします。
    /// </summary>
    /// <param name="clipName">再生するクリップ名</param>
    public void Play(string clipName)
    {
        if (!string.Equals(CurrentClip, clipName, StringComparison.OrdinalIgnoreCase))
        {
            CurrentClip = clipName;
            Timer = 0.0f;
            CurrentFrameIndex = 0;
            IsFinished = false;
        }

        IsPlaying = true;
    }
}
