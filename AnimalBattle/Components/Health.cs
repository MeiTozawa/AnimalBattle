using System;

namespace AnimalBattle.Components;

/// <summary>
/// エンティティ（プレイヤー、Boss 等）の現在 HP、最大 HP、および無敵時間を管理する汎用体力コンポーネント。
/// 単一責任の原則に従い、体力および被弾保護状態を独立して表現します。
/// </summary>
public struct Health
{
    /// <summary>現在の残り HP</summary>
    public float Current;

    /// <summary>最大 HP</summary>
    public float Max;

    /// <summary>被弾後の無敵時間タイマー（秒）</summary>
    public float InvincibleTimer;

    /// <summary>生存中かどうかを取得します。</summary>
    public readonly bool IsAlive => Current > 0.0f;

    /// <summary>無敵状態中かどうかを取得します。</summary>
    public readonly bool IsInvincible => InvincibleTimer > 0.0f;

    /// <summary>残り HP の割合（0.0f ~ 1.0f）を取得します。</summary>
    public readonly float Ratio => Max > 0.0f ? Math.Clamp(Current / Max, 0.0f, 1.0f) : 0.0f;

    /// <summary>
    /// <see cref="Health"/> 構造体の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="max">最大 HP</param>
    /// <param name="current">初期現在 HP（省略時は最大 HP と同値）</param>
    public Health(float max, float? current = null)
    {
        Max = max;
        Current = current ?? max;
        InvincibleTimer = 0.0f;
    }
}
