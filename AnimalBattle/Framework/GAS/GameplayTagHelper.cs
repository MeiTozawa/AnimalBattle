namespace AnimalBattle.Abilities;

/// <summary>
///     文字列コレクションから型安全な GameplayTag フラグマスクを解析・合成するヘルパークラス。
/// </summary>
public static class GameplayTagHelper {
    /// <summary>
    ///     タグ名リストからビットマスク形式の GameplayTag を解析して合成します。
    /// </summary>
    public static GameplayTag ParseTags(IEnumerable<string>? tagNames)
    {
        if (tagNames == null) return GameplayTag.None;
        var result = GameplayTag.None;
        foreach (var name in tagNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (Enum.TryParse<GameplayTag>(name.Trim(), ignoreCase: true, out var tag))
            {
                result |= tag;
            }
        }
        return result;
    }
}
