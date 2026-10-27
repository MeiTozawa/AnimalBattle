using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;

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
        BossType.Boss1 => nameof(BossType.Boss1),
        BossType.Boss2 => nameof(BossType.Boss2),
        _ => "Unknown"
    };

    /// <summary>Boss 1 状態の識別文字列を取得します。</summary>
    public static string Get(Boss1State state) => state switch {
        Boss1State.Hover => nameof(Boss1State.Hover),
        Boss1State.AttackSelection => nameof(Boss1State.AttackSelection),
        Boss1State.Telegraph => nameof(Boss1State.Telegraph),
        Boss1State.Execute => nameof(Boss1State.Execute),
        Boss1State.Recovery => nameof(Boss1State.Recovery),
        Boss1State.PhaseTransition => nameof(Boss1State.PhaseTransition),
        Boss1State.Dead => nameof(Boss1State.Dead),
        Boss1State.None => nameof(Boss1State.None),
        _ => "Unknown"
    };

    /// <summary>Boss 2 状態の識別文字列を取得します。</summary>
    public static string Get(Boss2State state) => state switch {
        Boss2State.Idle => nameof(Boss2State.Idle),
        Boss2State.PatrolChase => nameof(Boss2State.PatrolChase),
        Boss2State.MoveToTakeoff => nameof(Boss2State.MoveToTakeoff),
        Boss2State.PlatformTraverse => nameof(Boss2State.PlatformTraverse),
        Boss2State.Telegraph => nameof(Boss2State.Telegraph),
        Boss2State.Execute => nameof(Boss2State.Execute),
        Boss2State.Stunned => nameof(Boss2State.Stunned),
        Boss2State.Recovery => nameof(Boss2State.Recovery),
        Boss2State.PhaseTransition => nameof(Boss2State.PhaseTransition),
        Boss2State.Dead => nameof(Boss2State.Dead),
        Boss2State.None => nameof(Boss2State.None),
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