using Flecs.NET.Core;

namespace AnimalBattle.Components;

/// <summary>
///     母弾の周囲を公転する子衛星弾幕のリンク関係と軌道幾何パラメータを表すコンポーネント。
/// </summary>
public struct OrbitSatellite {
    /// <summary>衛星弾幕として現在アクティブに公転中かどうかのフラグ。</summary>
    public bool IsActive;

    /// <summary>公転中心となる親エンティティ</summary>
    public Entity Parent;

    /// <summary>親弾中心からの公転軌道半径</summary>
    public float OrbitDistance;

    /// <summary>初期公転角オフセットラジアン</summary>
    public float InitialAngle;

    /// <summary>生成時に記録した親エンティティのプール貸出世代バージョン。</summary>
    public uint ParentVersion;

    /// <summary>
    ///     公転衛星パラメータを指定してインスタンスを初期化します。
    /// </summary>
    /// <param name="isActive">アクティブ状態フラグ。</param>
    /// <param name="parent">公転中心エンティティ。</param>
    /// <param name="orbitDistance">軌道半径距離。</param>
    /// <param name="initialAngle">初期角度オフセット。</param>
    /// <param name="parentVersion">親エンティティの貸出世代バージョン。</param>
    public OrbitSatellite(bool isActive, Entity parent, float orbitDistance, float initialAngle = 0.0f, uint parentVersion = 0)
    {
        IsActive = isActive;
        Parent = parent;
        OrbitDistance = orbitDistance;
        InitialAngle = initialAngle;
        ParentVersion = parentVersion;
    }

    /// <summary>
    ///     公転衛星パラメータを指定してアクティブなインスタンスを初期化します。
    /// </summary>
    /// <param name="parent">公転中心エンティティ。</param>
    /// <param name="orbitDistance">軌道半径距離。</param>
    /// <param name="initialAngle">初期角度オフセット。</param>
    /// <param name="parentVersion">親エンティティの貸出世代バージョン。</param>
    public OrbitSatellite(Entity parent, float orbitDistance, float initialAngle = 0.0f, uint parentVersion = 0)
        : this(true, parent, orbitDistance, initialAngle, parentVersion)
    {
    }
}
