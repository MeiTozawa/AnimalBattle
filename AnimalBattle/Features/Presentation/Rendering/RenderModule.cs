namespace AnimalBattle.Presentation.Rendering;

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

        // 照準器スプリング物理更新
        world.AddAimReticleUpdateSystem();

        // 1. ワールド空間描画前のカメラオフセット適用
        world.AddCameraOffsetApplySystem();

        // 2. ワールド空間描画：タイルマップ、通常スプライト、雑魚敵HPバー、無敵点滅スプライト、弾幕、予兆、VFX、デバッグ枠
        world.AddTileMapRenderSystem()
             .AddSpriteRenderSystem()
             .AddMinionHpBarRenderSystem()
             .AddInvincibleBlinkRenderSystem()
             .AddBulletBatchRenderSystem()
             .AddBossTelegraphRenderSystem()
             .AddVfxRenderSystem()
             .AddDebugGizmosRenderSystem();

        // 3. スクリーン空間描画前のカメラオフセットリセット HUDや照準器およびUIへのシェイク波及を防止します
        world.AddCameraOffsetResetSystem();

        // 4. スクリーン空間描画：HUD、照準器、デバッグオーバーレイ
        world.AddHudRenderSystem()
             .AddAimReticleRenderSystem()
             .AddDebugOverlayRenderSystem();
    }
}
