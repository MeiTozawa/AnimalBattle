namespace AnimalBattle.Inputs;

/// <summary>
/// 入力コンテキスト（入力状態・モード）を定義する列挙型。
/// コンテキストに応じて有効なアクションマッピングがフィルタリングされます。
/// </summary>
public enum InputContext
{
    /// <summary>ゲームプレイ中コンテキスト</summary>
    InGame,

    /// <summary>メニュー / 設定画面コンテキスト</summary>
    Menu,

    /// <summary>ポーズ（一時停止）コンテキスト</summary>
    Pause
}
