namespace AnimalBattle.Plugins;

/// <summary>
/// 関連する複数の <see cref="IPlugin"/> を一括して登録・管理するためのプラグイングループインターフェース。
/// </summary>
public interface IPluginGroup
{
    /// <summary>
    /// グループに含まれるプラグイン群を App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    void Build(App app);
}
