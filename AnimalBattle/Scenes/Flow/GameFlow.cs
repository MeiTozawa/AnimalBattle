using AnimalBattle.Ai;
using AnimalBattle.Configs;

namespace AnimalBattle.Scenes;

/// <summary>
///     ゲーム全体の進行フローと選択状態を管理する ECS シングルトンコンポーネント。
/// </summary>
public struct GameFlow {
    public GameState State;
    public CharacterType SelectedPlayer;
    public BossType SelectedBoss;
}
