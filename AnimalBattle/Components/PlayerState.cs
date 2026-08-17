namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーの射撃クールダウンおよび死亡タイマーを保持するコンポーネント。
/// 体力は <see cref="Health"/>、二段跳躍は <see cref="DoubleJump"/> コンポーネントに委譲されます。
/// </summary>
public struct PlayerState
{
    /// <summary>手銃射撃のクールダウンタイマー（秒）</summary>
    public float ShootCooldown;

    /// <summary>死亡演出タイマー（秒）</summary>
    public float DeathTimer;

    /// <summary>
    /// <see cref="PlayerState"/> の新しいインスタンスを初期化します。
    /// </summary>
    public PlayerState()
    {
        ShootCooldown = 0.0f;
        DeathTimer = 0.0f;
    }
}
