namespace AnimalBattle.Plugins;

/// <summary>
/// Native C++ 描画 API サービス連携および 2D スプライト描画システムを一括構築するドメインプラグイン。
/// </summary>
public class RenderPlugin : IPlugin
{
    /// <summary>
    /// 画面描画および HUD デバッグ表示システムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddSystem(RenderSystems.RenderSystem)
           .AddSystem(RenderSystems.DebugInfoSystem);
    }
}
