namespace AnimalBattle.Player.Components;

/// <summary>
///     自機エンティティのキャラクター種別を保持するコンポーネント（HUD、描画、システム識別用）。
/// </summary>
public readonly struct CharacterTypeComponent(CharacterType type)
{
    /// <summary>キャラクター種別（Cat / Bunny 等）。</summary>
    public readonly CharacterType Value = type;
}
