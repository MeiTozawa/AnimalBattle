namespace AnimalBattle.Plugins;

/// <summary>
/// カメラビューポート管理およびターゲット追従計算システムを一括構築するドメインプラグイン。
/// </summary>
public class CameraPlugin : IPlugin
{
    /// <summary>
    /// カメラ追従システムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddSystem(CameraSystems.CameraSystem);
    }
}
