namespace AnimalBattle.Configs;

using Flecs.NET.Core;
using Microsoft.Extensions.Configuration;

/// <summary>
/// 全設定コンテナの動的登録、型安全な取得、および Flecs.NET ワールドへの一括注入を担う汎用登録センター。
/// </summary>
public interface IConfigRegistry : IDisposable
{
    /// <summary>
    /// TOML 設定ファイルを監視するローダーを伴って設定コンテナを登録します。
    /// </summary>
    ConfigHolder<T> RegisterToml<T>(
        string relativePath,
        Action<IConfiguration, T> binder,
        Action<World, T>? onReloaded = null) where T : class, new();

    /// <summary>
    /// 事前構築された設定インスタンスをそのまま登録します。
    /// </summary>
    ConfigHolder<T> RegisterInstance<T>(T instance) where T : class;

    /// <summary>
    /// 型に対応する設定ホルダーを取得します。
    /// </summary>
    ConfigHolder<T> GetHolder<T>() where T : class;

    /// <summary>
    /// 型に対応する現在の設定スナップショットを取得します。
    /// </summary>
    T Get<T>() where T : class;

    /// <summary>
    /// 指定された型が登録されているか判定します。
    /// </summary>
    bool Contains<T>() where T : class;

    /// <summary>
    /// 登録済みの全設定スナップショットを Flecs.NET ワールドへ単例コンポーネントとして一括注入します。
    /// </summary>
    void InjectAllToWorld(World world);
}
