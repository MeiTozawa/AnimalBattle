using AnimalBattle.Presentation.Animation;

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
}
