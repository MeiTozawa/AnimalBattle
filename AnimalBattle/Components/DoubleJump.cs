namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーおよびキャラクターの空中多段・二段跳躍（Double Jump）データを保持する純粋データコンポーネント。
/// </summary>
/// <remarks>
/// <see cref="DoubleJump"/> 構造体の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="maxJumps">最大跳躍可能回数</param>
public struct DoubleJump(int maxJumps = 2)
{
    /// <summary>残りの空中跳躍可能回数</summary>
    public int RemainingJumps = maxJumps;

    /// <summary>最大空中跳躍可能回数（標準は 2 回）</summary>
    public int MaxJumps = maxJumps;

    /// <summary>空中跳躍が可能かどうかを取得します。</summary>
    public readonly bool CanJump => RemainingJumps > 0;
}

