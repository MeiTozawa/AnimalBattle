namespace AnimalBattle.Inputs;

/// <summary>
/// ゲーム内で使用される高レベルな意味論的アクション (Semantic Actions) を定義する列挙型。
/// 生の物理キーコードやボタンから独立した抽象アクションを表します。
/// </summary>
public enum InputAction
{
    /// <summary>左移動アクション</summary>
    MoveLeft,

    /// <summary>右移動アクション</summary>
    MoveRight,

    /// <summary>上移動アクション</summary>
    MoveUp,

    /// <summary>下移動 / 急降下アクション</summary>
    MoveDown,

    /// <summary>ジャンプアクション</summary>
    Jump,

    /// <summary>攻撃アクション</summary>
    Attack,

    /// <summary>決定 / 選択確定アクション</summary>
    Confirm,

    /// <summary>キャンセル / 戻るアクション</summary>
    Cancel,

    /// <summary>メニュー表示 / ポーズ切り替えアクション</summary>
    ToggleMenu,

    /// <summary>デバッグ表示トグルアクション</summary>
    DebugToggle,

    /// <summary>プレイヤー人数 1 人切り替え</summary>
    PlayerCount1,

    /// <summary>プレイヤー人数 2 人切り替え</summary>
    PlayerCount2,

    /// <summary>プレイヤー人数 3 人切り替え</summary>
    PlayerCount3,

    /// <summary>プレイヤー人数 4 人切り替え</summary>
    PlayerCount4
}
