namespace AnimalBattle.Plugins;

/// <summary>
/// Bevy の <c>DefaultPlugins</c> に相当し、コア領域の標準プラグイン群（Require, Physics, Render, Camera, Character）を一括登録するプラグイングループクラス。
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
           .AddPlugin(new PhysicsPlugin())
           .AddPlugin(new RenderPlugin())
           .AddPlugin(new CameraPlugin())
           .AddPlugin(new CharacterPlugin())
           .AddPlugin(new EffectPlugin())
           .AddPlugin(new CombatPlugin());
    }
}
