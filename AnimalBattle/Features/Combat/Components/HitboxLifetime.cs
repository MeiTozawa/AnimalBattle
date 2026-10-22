namespace AnimalBattle.Components;

/// <summary>
///     攻撃判定実体：近接斬撃等の持続時間および経過時間を管理する純粋データコンポーネント。
/// </summary>
public struct HitboxLifetime(float duration)
{
    /// <summary>判定の総持続時間秒単位。</summary>
    public float Duration = duration;

    /// <summary>生成からの経過時間秒単位。</summary>
    public float ElapsedTimer = 0.0f;

    /// <summary>持続時間が満了したか否かを取得します。</summary>
    public readonly bool IsFinished => ElapsedTimer >= Duration;
}
