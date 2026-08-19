using Flecs.NET.Core;

namespace AnimalBattle.Plugins;

/// <summary>
/// Bevy の Required Components 属性 (<c>[Require&lt;T&gt;]</c>) が付与された型に対し、
/// Flecs.NET の <c>With</c> トレイトによる必須コンポーネント自動補完を登録するコアプラグイン。
/// </summary>
/// <remarks>
/// 対象型の走査は <c>RequireComponentGenerator</c> (Incremental Source Generator) が
/// コンパイル時に行い、<c>RequireComponentRegistry.g.cs</c> として静的なジェネリック呼び出しの
/// みで構成される登録コードを自動生成します。このプロジェクトは <c>PublishAot</c> を有効化しているため、
/// 実行時リフレクション (<c>Assembly.GetTypes()</c> / <c>MethodInfo.MakeGenericMethod()</c>) による
/// 走査はトリミング / Native AOT 環境で動作が保証されません (IL2026 / IL2060 / IL3050)。
/// <c>[Require&lt;T&gt;]</c> を新しい型に付与するだけで、本プラグインを含めどこも手動で
/// 変更する必要はありません。
/// </remarks>
public class RequirePlugin : IPlugin
{
    /// <summary>
    /// コンパイル時に自動生成された Require 関係を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        RequireComponentRegistry.RegisterAll(app.World);
    }
}
