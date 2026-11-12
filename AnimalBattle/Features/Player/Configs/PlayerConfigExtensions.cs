namespace AnimalBattle.Configs;

using System.IO;

/// <summary>
///     Flecs.NET ワールドから武器・特殊能力設定を取得・解決する拡張メソッド群。
/// </summary>
public static class PlayerConfigExtensions
{
    /// <summary>
    ///     プレイヤー設定に指定された武器名から武器設定スナップショットを動的に解決します。
    ///     キャラクター種別のハードコードを排除し、設定ファイル駆動の武器バインドを提供します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="weaponName">武器設定名または TOML ファイル名。</param>
    /// <returns>解決された <see cref="IWeaponConfig"/> インスタンス。</returns>
    public static IWeaponConfig GetWeaponConfig(this World world, string weaponName)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        var normalized = weaponName.Trim();
        if (normalized.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
        {
            normalized = Path.GetFileNameWithoutExtension(normalized);
        }
        return normalized switch
        {
            "CatWeapon" => world.Get<CatWeaponConfig>(),
            "BunnyWeapon" => world.Get<BunnyWeaponConfig>(),
            _ => AssertHelper.FailWithDefault($"未知の武器設定です: {weaponName}", (IWeaponConfig)world.Get<CatWeaponConfig>())
        };
    }
}
