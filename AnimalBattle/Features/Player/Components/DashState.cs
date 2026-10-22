namespace AnimalBattle.Player.Components;

/// <summary>
///     自機のダッシュ・回避状態コンポーネント。
///     ダッシュ持続時間、クールダウン、突進方向および残像生成タイマーを一元管理します。
/// </summary>
public struct DashState
{
    /// <summary>ダッシュ持続タイマー秒単位。0 より大きい場合はダッシュ中。</summary>
    public float Timer;

    /// <summary>ダッシュ再使用クールダウンタイマー秒単位。</summary>
    public float Cooldown;

    /// <summary>ダッシュ突進 X 軸方向：-1.0f または 1.0f。</summary>
    public float DirectionX;

    /// <summary>残像生成用インターバルタイマー秒単位。</summary>
    public float GhostTrailTimer;

    /// <summary>現在ダッシュ中であるかどうか。</summary>
    public readonly bool IsDashing => Timer > 0.0f;
}
