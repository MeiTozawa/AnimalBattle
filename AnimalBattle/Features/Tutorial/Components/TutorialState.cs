namespace AnimalBattle.Features.Tutorial.Components;

/// <summary>
///     チュートリアルの進行状態とタイマーを保持するワールドシングルトンコンポーネント。
/// </summary>
public struct TutorialState
{
    /// <summary>現在実行中のチュートリアルフェーズ番号（0オリジン）。</summary>
    public int CurrentPhaseIndex;

    /// <summary>現在フェーズの残り目標数（コイン残数または生存小怪数）。</summary>
    public int TargetsRemaining;

    /// <summary>現在フェーズの目標が達成されたかどうか。</summary>
    public bool IsPhaseCompleted;

    /// <summary>目標達成後、次フェーズへ遷移するまでの猶予タイマー（秒）。</summary>
    public float CompletionTimer;

    /// <summary>リトライ処理中フラグ。</summary>
    public bool IsRetrying;

    /// <summary>リトライ演出タイマー（秒）。</summary>
    public float RetryTimer;
}
