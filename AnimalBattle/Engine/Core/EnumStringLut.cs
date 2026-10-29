using AnimalBattle.Ai;
using AnimalBattle.Ai.BossSnake;
using AnimalBattle.Ai.BossLion;

namespace AnimalBattle.Core;

/// <summary>
///     HUD およびデバッグ表示用の列挙体文字列変換ヘルパー。
/// </summary>
public static class EnumStringLut {
    /// <summary>キャラクター種類の識別文字列を取得します。</summary>
    public static string Get(CharacterType characterType) => characterType switch {
        CharacterType.Cat => nameof(CharacterType.Cat),
        CharacterType.Bunny => nameof(CharacterType.Bunny),
        _ => "Unknown"
    };

    /// <summary>Boss 種類の識別文字列を取得します。</summary>
    public static string Get(BossType bossType) => bossType switch {
        BossType.BossSnake => nameof(BossType.BossSnake),
        BossType.BossLion => nameof(BossType.BossLion),
        _ => "Unknown"
    };

    /// <summary>BossSnake 状態の識別文字列を取得します。</summary>
    public static string Get(BossSnakeState state) => state switch {
        BossSnakeState.Hover => nameof(BossSnakeState.Hover),
        BossSnakeState.AttackSelection => nameof(BossSnakeState.AttackSelection),
        BossSnakeState.Telegraph => nameof(BossSnakeState.Telegraph),
        BossSnakeState.Execute => nameof(BossSnakeState.Execute),
        BossSnakeState.Recovery => nameof(BossSnakeState.Recovery),
        BossSnakeState.PhaseTransition => nameof(BossSnakeState.PhaseTransition),
        BossSnakeState.Dead => nameof(BossSnakeState.Dead),
        BossSnakeState.None => nameof(BossSnakeState.None),
        _ => "Unknown"
    };

    /// <summary>BossLion 状態の識別文字列を取得します。</summary>
    public static string Get(BossLionState state) => state switch {
        BossLionState.Idle => nameof(BossLionState.Idle),
        BossLionState.PatrolChase => nameof(BossLionState.PatrolChase),
        BossLionState.MoveToTakeoff => nameof(BossLionState.MoveToTakeoff),
        BossLionState.PlatformTraverse => nameof(BossLionState.PlatformTraverse),
        BossLionState.Telegraph => nameof(BossLionState.Telegraph),
        BossLionState.Execute => nameof(BossLionState.Execute),
        BossLionState.Stunned => nameof(BossLionState.Stunned),
        BossLionState.Recovery => nameof(BossLionState.Recovery),
        BossLionState.PhaseTransition => nameof(BossLionState.PhaseTransition),
        BossLionState.Dead => nameof(BossLionState.Dead),
        BossLionState.None => nameof(BossLionState.None),
        _ => "Unknown"
    };

    /// <summary>戦闘 Phase の識別文字列を取得します。</summary>
    public static string Get(Phase phase) => phase switch {
        Phase.Phase1 => "Phase 1",
        Phase.Phase2 => "Phase 2",
        Phase.Phase3 => "Phase 3",
        _ => "Unknown"
    };

}