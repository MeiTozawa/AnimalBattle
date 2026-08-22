namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// State module のライフサイクル呼び出し種別を定義します。
/// </summary>
public enum LifecycleOperation
{
    /// <summary>
    /// 状態へ入場する処理を示します。
    /// </summary>
    Enter = 0,

    /// <summary>
    /// 状態をフレーム更新する処理を示します。
    /// </summary>
    Update = 1,

    /// <summary>
    /// 状態から退場する処理を示します。
    /// </summary>
    Exit = 2
}
