namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーおよびキャラクターの空中多段・二段跳躍（Double Jump）を管理する独立コンポーネント。
/// 単一責任の原則に従い、跳躍リソースの残数とリセット処理を自己完結して表現します。
/// </summary>
public struct DoubleJump
{
    /// <summary>残りの空中跳躍可能回数</summary>
    public int RemainingJumps;

    /// <summary>最大空中跳躍可能回数（標準は 2 回）</summary>
    public int MaxJumps;

    /// <summary>跳躍可能かどうかを取得します。</summary>
    public readonly bool CanJump => RemainingJumps > 0;

    /// <summary>
    /// <see cref="DoubleJump"/> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="maxJumps">最大跳躍可能回数</param>
    public DoubleJump(int maxJumps = 2)
    {
        MaxJumps = maxJumps;
        RemainingJumps = maxJumps;
    }

    /// <summary>
    /// 接地時に跳躍可能回数を最大値へリセットします。
    /// </summary>
    public void Reset()
    {
        RemainingJumps = MaxJumps;
    }
}
