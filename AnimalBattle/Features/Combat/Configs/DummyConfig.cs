using System.Diagnostics.CodeAnalysis;

namespace AnimalBattle.Configs;

/// <summary>
///     Config/dummy.toml から読み込まれるトレーニング木桩設定クラス。
///     初期配置、寸法、無敵・受撃演出時間およびキネマティック物理特性を管理します。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class DummyConfig
{
    /// <summary>初期スポーン座標 Vec2f型。</summary>
    public Vec2f Spawn { get; set; }

    /// <summary>足場衝突および被弾判定矩形サイズ Vec2f型。</summary>
    public Vec2f CollisionSize { get; set; }

    /// <summary>木桩の初期最大 HP。</summary>
    public float Health { get; set; }

    /// <summary>被弾時ホワイトフラッシュ演出の持続秒数。</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>空中にいる場合の下向き重力加速度（ピクセル/秒²）。</summary>
    public float Gravity { get; set; }

    /// <summary>落下時の終端落下速度上限（ピクセル/秒）。</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>被弾時ノックバック外力の減衰速度（ピクセル/秒²）。</summary>
    public float KnockbackDeceleration { get; set; }

    /// <summary>接地時の水平速度摩擦減速度（ピクセル/秒²）。</summary>
    public float GroundFrictionDeceleration { get; set; }

    /// <summary>空中浮遊時の水平速度空気抵抗減速度（ピクセル/秒²）。</summary>
    public float AirDragDeceleration { get; set; }
}
