namespace AnimalBattle.Common.Components;

/// <summary>
///     エンティティの生命値コンポーネント：純粋なデータ構造体。
///     ECS 設計原則に従い、振る舞い・メソッドを持たずデータのみを保持します。
/// </summary>
public struct Health
{
    /// <summary>最大 HP。</summary>
    public float Max;

    /// <summary>現在の HP。</summary>
    public float Current;

    public Health(float max, float? current = null)
    {
        Max = Math.Max(1.0f, max);
        Current = Math.Clamp(current ?? max, 0.0f, Max);
    }
}
