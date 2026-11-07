namespace AnimalBattle.Combat.Components;

/// <summary>
///     接触持続時間に応じて多段ダメージ・跳字を発生させる特殊弾頭コンポーネント。
/// </summary>
public struct ContinuousDamageDealer
{
    /// <summary>1秒あたりの期待総ダメージ量</summary>
    public float DamagePerSecond;

    /// <summary>ヒット判定・ダメージティック間隔（秒）</summary>
    public float TickInterval;

    /// <summary>次回のダメージ発生までの残り時間タイマー（秒）</summary>
    public float NextTickTimer;
}
