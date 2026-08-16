namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーのジャンプ回数、射撃クールダウン、および生存状態を保持するコンポーネント。
/// </summary>
public struct PlayerState
{
    /// <summary>残りの跳躍可能回数（空中二段ジャンプ用）</summary>
    public int JumpCount;

    /// <summary>最大跳躍可能回数（標準は 2 回）</summary>
    public int MaxJumps;

    /// <summary>手銃射撃のクールダウンタイマー（秒）</summary>
    public float ShootCooldown;

    /// <summary>プレイヤーの生存フラグ</summary>
    public bool IsAlive;

    /// <summary>死亡演出タイマー（秒）</summary>
    public float DeathTimer;

    /// <summary>
    /// <see cref="PlayerState"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="maxJumps">最大跳躍回数</param>
    public PlayerState(int maxJumps = 2)
    {
        MaxJumps = maxJumps;
        JumpCount = maxJumps;
        ShootCooldown = 0.0f;
        IsAlive = true;
        DeathTimer = 0.0f;
    }
}
