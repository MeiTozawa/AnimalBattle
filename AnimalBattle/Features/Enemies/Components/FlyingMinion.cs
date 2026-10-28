namespace AnimalBattle.Enemies.Components;

/// <summary>
/// 空中浮遊弾幕怪を識別および状態管理するコンポーネント。
/// </summary>
public struct FlyingMinion
{
    /// <summary>正弦波浮遊用の経過タイマー。単位は秒。</summary>
    public float HoverTimer;

    /// <summary>アビリティ実行を管理するコンポーネント。</summary>
    public AbilitySystemComponent? Asc;

    /// <summary>
    /// 空中浮遊弾幕怪コンポーネントを初期化します。
    /// </summary>
    /// <param name="asc">アビリティシステムコンポーネント</param>
    public FlyingMinion(AbilitySystemComponent? asc = null)
    {
        HoverTimer = 0f;
        Asc = asc;
    }
}
