namespace AnimalBattle.Characters;

/// <summary>
///     キャラクターの体力値および被弾無敵タイマーを管理する純粋なドメインモデル。
/// </summary>
public sealed class Health {
    /// <summary>最大 HP。</summary>
    public float Max { get; set; }

    /// <summary>現在の HP。</summary>
    public float Current { get; set; }

    /// <summary>被弾時等の無敵時間タイマー（秒）。0 超で無敵状態。</summary>
    public float InvincibleTimer { get; set; }

    /// <summary>無敵時間中であるかを取得します。</summary>
    public bool IsInvincible => InvincibleTimer > 0.0f;

    /// <summary>生存中であるかを取得します。</summary>
    public bool IsAlive => Current > 0.0f;

    /// <summary>
    ///     <see cref="Health"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="max">最大 HP。</param>
    /// <param name="current">初期 HP（null の場合は最大 HP と同値）。</param>
    public Health(float max, float? current = null)
    {
        Max = Math.Max(1.0f, max);
        Current = Math.Clamp(current ?? max, 0.0f, Max);
        InvincibleTimer = 0.0f;
    }

    /// <summary>
    ///     無敵タイマーを経過時間分減衰させます。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    public void UpdateInvincibility(float deltaTime)
    {
        if (InvincibleTimer > 0.0f)
        {
            InvincibleTimer = Math.Max(0.0f, InvincibleTimer - deltaTime);
        }
    }
}
