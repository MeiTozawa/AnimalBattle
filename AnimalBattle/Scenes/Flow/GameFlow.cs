using AnimalBattle.Ai;

namespace AnimalBattle.Scenes;

/// <summary>
///     ゲーム全体の進行フローと選択状態を管理する ECS シングルトンコンポーネント。
/// </summary>
public struct GameFlow {
    public GameState State;
    public CharacterType SelectedPlayer;
    public BossType SelectedBoss;

    /// <summary>
    ///     戦闘またはチュートリアルのゲームプレイ中であるかを判定します。
    /// </summary>
    public readonly bool IsGameplay => State is GameState.Battle or GameState.Tutorial;
}
