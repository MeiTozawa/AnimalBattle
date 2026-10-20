using System.Runtime.InteropServices;

namespace AnimalBattle.Common.Components;

/// <summary>
///     移動速度およびノックバック速度を表すコンポーネント。
///     Native C++ の ab::Velocity とメモリレイアウトが完全一致（16 bytes）します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Velocity {
    /// <summary>移動速度 (Vec2f)</summary>
    public Vec2f Value;

    /// <summary>ノックバック速度 (Vec2f)</summary>
    public Vec2f Knockback;

    /// <summary>
    ///     指定された移動速度ベクトル、ノックバックベクトルで <see cref="Velocity" /> の新しいインスタンスを初期化します。
    /// </summary>
    public Velocity(Vec2f value, Vec2f knockback = default)
    {
        Value = value;
        Knockback = knockback;
    }
}