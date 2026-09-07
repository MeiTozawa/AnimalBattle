using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Components;

/// <summary>
/// アニメーション再生状態およびキャラクター種別を保持する純粋コンポーネント。
/// 全フィールドがアンマネージド型で構成され、GC 追跡オーバーヘッドをゼロにします。
/// </summary>
/// <remarks>
/// 指定された初期クリップ識別子およびキャラクター種別で <see cref="Animation"/> の新しいインスタンスを初期化します。
/// </remarks>
public struct Animation(StringId defaultClip = default, CharacterType characterType = CharacterType.Cat, bool isPlaying = true)
{
    /// <summary>デフォルトのアニメーションクリップ識別子 ("Idle")</summary>
    public static readonly StringId DefaultClip = "Idle";

    /// <summary>
    /// デフォルト設定で <see cref="Animation"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Animation() : this(default, CharacterType.Cat, true)
    {
    }

    /// <summary>再生対象のキャラクター種別</summary>
    public CharacterType CharacterType = characterType;

    /// <summary>再生中のアニメーションクリップ識別子</summary>
    public StringId CurrentClip = defaultClip.IsEmpty ? DefaultClip : defaultClip;

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
    /// <param name="clipId">再生するクリップ識別子</param>
    public void Play(StringId clipId)
    {
        if (CurrentClip != clipId)
        {
            CurrentClip = clipId;
            Timer = 0.0f;
            CurrentFrameIndex = 0;
            IsFinished = false;
        }

        IsPlaying = true;
    }
}
