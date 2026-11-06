namespace AnimalBattle.Features.Tutorial.Components;

/// <summary>
///     チュートリアルの進行状態とタイマーを保持するワールドシングルトンコンポーネント。
/// </summary>
public struct TutorialState
{
    /// <summary>現在実行中のチュートリアルフェーズ番号（0オリジン）。</summary>
    public int CurrentPhaseIndex;

    /// <summary>現在フェーズ内で実行中のステップ番号（0オリジン）。</summary>
    public int CurrentStepIndex;

    /// <summary>現在ステップの残り目標数（コイン残数または生存小怪数）。</summary>
    public int TargetsRemaining;

    /// <summary>ステップ開始時からの累積経過タイマー（秒）。</summary>
    public float StepTimer;

    /// <summary>ステップ完了判定後の猶予演出タイマー（秒）。</summary>
    public float StepCompletionTimer;

    /// <summary>現在フェーズの全目標が達成されたかどうか。</summary>
    public bool IsPhaseCompleted;

    /// <summary>フェーズ完了後、次フェーズへ遷移するまでの猶予タイマー（秒）。</summary>
    public float CompletionTimer;

    /// <summary>リトライ処理中フラグ。</summary>
    public bool IsRetrying;

    /// <summary>リトライ演出タイマー（秒）。</summary>
    public float RetryTimer;
}
