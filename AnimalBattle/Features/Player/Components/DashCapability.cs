namespace AnimalBattle.Player.Components;

using System.Diagnostics;
using AnimalBattle.Configs;

/// <summary>
///     キャラクターの突進回避（ダッシュ）および残像演出能力コンポーネント。
/// </summary>
public struct DashCapability
{
    /// <summary>突進速度：px/s</summary>
    public float Speed;

    /// <summary>突進持続時間秒単位</summary>
    public float Duration;

    /// <summary>再使用クールダウン時間秒単位</summary>
    public float Cooldown;

    /// <summary>突進中の無敵時間秒単位</summary>
    public float InvincibleDuration;

    /// <summary>突進開始時のストレッチ変形倍率</summary>
    public Vec2f StretchScale;

    /// <summary>残像生成インターバル秒単位</summary>
    public float GhostTrailInterval;

    /// <summary>残像のフェード持続時間秒単位</summary>
    public float GhostTrailDuration;

    /// <summary>残像の初期アルファ値：0〜255</summary>
    public float GhostTrailAlpha;

    /// <summary>
    ///     設定オブジェクトから突進能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">突進設定</param>
    /// <returns>構築された DashCapability インスタンス</returns>
    public static DashCapability FromConfig(PlayerDashConfig cfg)
    {
        Debug.Assert(cfg != null, "PlayerDashConfig は null であってはなりません。");
        return new DashCapability
        {
            Speed = cfg.Speed,
            Duration = cfg.Duration,
            Cooldown = cfg.Cooldown,
            InvincibleDuration = cfg.InvincibleDuration,
            StretchScale = cfg.StretchScale,
            GhostTrailInterval = cfg.GhostTrailInterval,
            GhostTrailDuration = cfg.GhostTrailDuration,
            GhostTrailAlpha = cfg.GhostTrailAlpha
        };
    }
}
