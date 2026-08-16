namespace AnimalBattle.Components;

/// <summary>
/// 移動速度およびノックバック速度を表すコンポーネント。
/// </summary>
public struct Velocity
{
    /// <summary>X 軸移動速度</summary>
    public float Vx;

    /// <summary>Y 軸移動速度</summary>
    public float Vy;

    /// <summary>X 軸ノックバック速度</summary>
    public float KnockbackX;

    /// <summary>Y 軸ノックバック速度</summary>
    public float KnockbackY;

    /// <summary>
    /// 指定された移動速度で <see cref="Velocity"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Velocity(float vx = 0.0f, float vy = 0.0f)
    {
        Vx = vx;
        Vy = vy;
        KnockbackX = 0.0f;
        KnockbackY = 0.0f;
    }
}
