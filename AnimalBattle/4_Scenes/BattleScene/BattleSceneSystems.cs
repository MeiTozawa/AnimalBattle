using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     戦闘シーンの入力処理システムおよび AI・カメラ推進システムを一括登録するオーケストレーター。
/// </summary>
public static class BattleSceneSystems {
    /// <summary>
    ///     戦闘シーンシステム群（入力処理および AI・カメラ）を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleSceneSystems(this World world)
    {
        return world.AddBattleInputSystem()
                    .AddBattleAiSystem();
    }
}
