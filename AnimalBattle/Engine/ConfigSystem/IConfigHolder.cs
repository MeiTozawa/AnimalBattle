namespace AnimalBattle.Configs;

using Flecs.NET.Core;

/// <summary>
/// 非ジェネリックな設定コンテナの共通操作インターフェース。
/// </summary>
public interface IConfigHolder : IDisposable
{
    /// <summary>設定の型を取得します。</summary>
    /// <summary>ボックス化された現在の設定値を取得します。</summary>
    /// <summary>設定バージョン番号を取得します。</summary>
    /// <summary>Flecs.NET ワールドへ現在の設定を注入します。</summary>
    void InjectToWorld(World world);
}
