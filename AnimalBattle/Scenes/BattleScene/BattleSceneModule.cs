using AnimalBattle.Abilities;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     戦闘シーンにおけるプレイヤー入力、ボス AI 思考、および動的カメラ追従システムを一括管理する Flecs シーンモジュール。
/// </summary>
public readonly struct BattleSceneModule : IFlecsModule {
    /// <summary>
    ///     戦闘シーンの ECS システム群：入力処理、ボス AI 思考、カメラ追従を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<BattleSceneModule>();

        world.AddBattleInputSystem()
             .AddBattleAiSystem();
    }
}
