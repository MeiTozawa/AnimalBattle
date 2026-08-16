namespace AnimalBattle.Plugins;

/// <summary>
/// コア領域の標準プラグイン群（Require, Effect, Bullet）を一括登録するプラグイングループクラス。
/// </summary>
public class DefaultPlugins : IPluginGroup
{
    /// <summary>
    /// 全コア標準プラグインを App へ順次登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddPlugin(new RequirePlugin())
           .AddPlugin(new EffectPlugin())
           .AddPlugin(new BulletPlugin());
    }
}
