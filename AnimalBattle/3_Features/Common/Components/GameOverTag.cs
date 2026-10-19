namespace AnimalBattle.Components;

/// <summary>
///     ゲームオーバー（勝利または敗北）状態を示すシングルトンタグコンポーネント。
///     このタグが存在する場合、ゲームの時間流速（deltaTime）は 0 に設定され、戦闘の更新が停止します。
/// </summary>
public struct GameOverTag { }
