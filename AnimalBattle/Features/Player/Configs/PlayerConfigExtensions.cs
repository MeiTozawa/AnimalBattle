namespace AnimalBattle.Configs;

using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

/// <summary>
///     Flecs.NET ワールドからプレイヤー設定を取得・解決する拡張メソッド群。
///     三目演算子による分岐を排除し、キャラクター種別の追加に対して開かれた設計を提供します。
/// </summary>
public static class PlayerConfigExtensions
{
    /// <summary>
    ///     指定されたキャラクター種別に対応するプレイヤー設定を取得します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="characterType">プレイヤーキャラクター種別。</param>
    /// <returns>対応する <see cref="PlayerConfig"/> インスタンス。</returns>
    public static PlayerConfig GetPlayerConfig(this World world, CharacterType characterType)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        return characterType switch
        {
            CharacterType.Cat => world.Get<CatConfig>(),
            CharacterType.Bunny => world.Get<BunnyConfig>(),
            _ => AssertHelper.FailWithDefault($"未対応のキャラクター種別です: {characterType}", (PlayerConfig)world.Get<CatConfig>())
        };
    }

    /// <summary>
    ///     現在アクティブ：GameFlow.SelectedPlayerなプレイヤー設定を取得します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <returns>現在選択中の <see cref="PlayerConfig"/> インスタンス。</returns>
    public static PlayerConfig GetActivePlayerConfig(this World world)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        ref readonly var flow = ref world.Get<GameFlow>();
        return world.GetPlayerConfig(flow.SelectedPlayer);
    }
}
