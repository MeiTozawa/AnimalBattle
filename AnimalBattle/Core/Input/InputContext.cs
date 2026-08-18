namespace AnimalBattle.Core.Input;

/// <summary>
/// 入力コンテキスト（入力状態の分離スタック用）。
/// </summary>
public enum InputContext
{
    InGame = 0,
    Menu = 1,
    Cutscene = 2,
    Pause = 3,
    VolumeControl = 4,
}
