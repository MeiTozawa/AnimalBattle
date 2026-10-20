namespace AnimalBattle.Combat.Components;

/// <summary>
///     母弾の周囲を公転する子衛星弾幕の軌道幾何パラメータを表す純粋データコンポーネント。
///     公転中心（母弾）との紐付けは Flecs 原生関係 (OrbitOf, ParentBullet) により管理されます。
/// </summary>
public struct OrbitSatellite(float orbitDistance = 0.0f, float initialAngle = 0.0f)
{
    /// <summary>親弾中心からの公転軌道半径</summary>
    public float OrbitDistance = orbitDistance;

    /// <summary>初期公転角オフセット（ラジアン）</summary>
    public float InitialAngle = initialAngle;
}
