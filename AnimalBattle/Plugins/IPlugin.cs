namespace AnimalBattle.Plugins;

/// <summary>
/// Bevy エンジンの <c>Plugin</c> アーキテクチャに準拠したモジュール拡張インターフェース。
/// ドメイン（Physics, Render, Character 等）ごとにシステムやリソースをカプセル化して登録します。
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// プラグイン固有の ECS システム、リソース、および依存コンポーネントを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    void Build(App app);

    /// <summary>
    /// 全プラグインの Build 完了後に実行される後処理。必要に応じてオーバーライドします。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    void Finish(App app)
    {
    }

    /// <summary>
    /// アプリケーション終了時のリソース破棄処理。必要に応じてオーバーライドします。
    /// </summary>
    /// <param name="app">破棄対象の App コンテナ</param>
    void Cleanup(App app)
    {
    }
}
