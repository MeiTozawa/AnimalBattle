using System.Runtime.InteropServices;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Components;

/// <summary>
/// 移動速度およびノックバック速度を表すコンポーネント。
/// Native C++ の ab::Velocity とメモリレイアウトが完全一致（16 bytes）します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Velocity
{
    /// <summary>移動速度 (Vec2f)</summary>
    public Vector2D Value;

    /// <summary>ノックバック速度 (Vec2f)</summary>
    public Vector2D Knockback;

    /// <summary>X 軸移動速度への簡易アクセスプロパティ</summary>
    public float Vx
    {
        readonly get => Value.X;
        set => Value = new Vector2D(value, Value.Y);
    }

    /// <summary>Y 軸移動速度への簡易アクセスプロパティ</summary>
    public float Vy
    {
        readonly get => Value.Y;
        set => Value = new Vector2D(Value.X, value);
    }

    /// <summary>X 軸ノックバック速度への簡易アクセスプロパティ</summary>
    public float KnockbackX
    {
        readonly get => Knockback.X;
        set => Knockback = new Vector2D(value, Knockback.Y);
    }

    /// <summary>Y 軸ノックバック速度への簡易アクセスプロパティ</summary>
    public float KnockbackY
    {
        readonly get => Knockback.Y;
        set => Knockback = new Vector2D(Knockback.X, value);
    }

    /// <summary>
    /// 指定された移動速度で <see cref="Velocity"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Velocity(float vx = 0.0f, float vy = 0.0f)
    {
        Value = new Vector2D(vx, vy);
        Knockback = Vector2D.Zero;
    }

    /// <summary>
    /// 指定された移動速度ベクトル、ノックバックベクトルで <see cref="Velocity"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Velocity(Vector2D value, Vector2D knockback = default)
    {
        Value = value;
        Knockback = knockback;
    }
}
