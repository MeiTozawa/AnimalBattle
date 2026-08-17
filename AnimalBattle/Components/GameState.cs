namespace AnimalBattle.Components;

/// <summary>
/// バトル全体のゲーム進行状態（ゲームオーバー、勝利判定）を保持するシングルトンコンポーネント。
/// </summary>
/// <remarks>
/// <see cref="GameState"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="isGameOver">ゲームオーバー状態</param>
/// <param name="isVictory">勝利状態</param>
public struct GameState(bool isGameOver = false, bool isVictory = false)
{
    /// <summary>プレイヤーが死亡しゲームオーバー中かどうかのフラグ</summary>
    public bool IsGameOver = isGameOver;

    /// <summary>Boss を撃破し勝利したかどうかのフラグ</summary>
    public bool IsVictory = isVictory;
}
