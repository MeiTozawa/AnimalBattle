namespace AnimalBattle.Characters;

/// <summary>
/// プレイヤーの入力・操作種別（人間操作、CPU操作等）を表す列挙型。
/// </summary>
public enum PlayerType
{
    /// <summary>
    /// 人間プレイヤー（キーボードまたはゲームパッドによる操作）。
    /// </summary>
    Human,

    /// <summary>
    /// CPU（AI）制御プレイヤー。
    /// </summary>
    Cpu
}
