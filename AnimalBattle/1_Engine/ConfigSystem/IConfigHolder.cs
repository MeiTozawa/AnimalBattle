namespace AnimalBattle.Configs;

using Flecs.NET.Core;

/// <summary>
/// 非ジェネリックな設定コンテナの共通操作インターフェース。
/// </summary>
public interface IConfigHolder : IDisposable
{
    /// <summary>設定の型を取得します。</summary>
    Type ConfigType { get; }

    /// <summary>ボックス化された現在の設定値を取得します。</summary>
    object BoxedValue { get; }

    /// <summary>設定バージョン番号を取得します。</summary>
    uint Version { get; }

    /// <summary>Flecs.NET ワールドへ現在の設定を注入します。</summary>
    void InjectToWorld(World world);
}
