using AnimalBattle.Ai;
using AnimalBattle.Configs;

namespace AnimalBattle.Components;

/// <summary>
///     ゲームの全体ライフサイクル状態。
/// </summary>
public enum GameState {
    Start = 0,
    Battle = 1,
    Paused = 2
}

/// <summary>
///     ゲーム全体の進行フローと選択状態を管理する ECS シングルトンコンポーネント。
/// </summary>
public struct GameFlow {
    public GameState State;
    public CharacterType SelectedPlayer;
    public BossType SelectedBoss;
}

/// <summary>
///     開始・選択シーンに属するエンティティを識別するタグコンポーネント。
/// </summary>
public struct StartSceneTag { }

/// <summary>
///     戦闘シーンに属するエンティティを識別するタグコンポーネント。
/// </summary>
public struct BattleSceneTag { }
