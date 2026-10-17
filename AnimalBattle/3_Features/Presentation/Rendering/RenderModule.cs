using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Systems.Render;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation;

/// <summary>
///     アリーナ足場、キャラクター、弾幕、エフェクト、HUD および照準器の描画システムを一括管理する描画モジュール。
///     Flecs.NET の Ecs.OnStore フェーズにて登録順序に従い、全描画処理を決定論的にパイプライン実行します。
/// </summary>
public readonly struct RenderModule : IFlecsModule {
    /// <summary>
    ///     全描画関連の ECS システムをワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<RenderModule>();

        // 1. ワールド空間描画前のカメラオフセット適用
        world.System("CameraOffsetApplySystem")
             .Kind(Ecs.OnStore)
             .Iter((Iter it) =>
             {
                 var w = it.World();
                 if (w.Get<GameFlow>().State == GameState.Battle)
                 {
                     GameEngine.SetCameraOffset(CameraService.RenderOffsetX, CameraService.RenderOffsetY);
                 }
                 else
                 {
                     GameEngine.SetCameraOffset(0, 0);
                 }
             });

        // 2. ワールド空間描画（タイルマップ、キャラクター、弾幕、予兆、VFX、デバッグ枠）
        world.AddTileMapRenderSystem()
             .AddSpriteRenderSystem()
             .AddBulletBatchRenderSystem()
             .AddBossTelegraphRenderSystem()
             .AddVfxRenderSystem()
             .AddDebugGizmosRenderSystem();

        // 3. スクリーン空間描画前のカメラオフセットリセット (HUD / 照準器 / UI へのシェイク波及防止)
        world.System("CameraOffsetResetSystem")
             .Kind(Ecs.OnStore)
             .Iter((Iter it) =>
             {
                 GameEngine.SetCameraOffset(0, 0);
             });

        // 4. スクリーン空間描画（HUD、照準器）
        world.AddHudRenderSystem()
             .AddAimReticleRenderSystem();
    }
}
