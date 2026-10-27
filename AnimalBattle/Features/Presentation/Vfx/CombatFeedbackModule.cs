using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     ヒットストップ：時間凍結および被弾局所振動：ジッターなどの戦闘手応え・演出フィードバックシステムを一括管理するモジュール。
/// </summary>
public readonly struct CombatFeedbackModule : IFlecsModule
{
    /// <summary>
    ///     戦闘フィードバック関連の ECS システム群をワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<CombatFeedbackModule>();

        world.AddHitstopSystem()
             .AddSpriteJitterSystem();
    }
}
