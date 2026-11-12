namespace AnimalBattle.Configs;

/// <summary>
///     プレイヤーの突進回避（ダッシュ）および残像演出設定。
/// </summary>
public sealed class PlayerDashConfig
{
    /// <summary>突進速度：px/s</summary>
    public float Speed { get; set; }

    /// <summary>突進持続時間秒単位</summary>
    public float Duration { get; set; }

    /// <summary>再使用クールダウン時間秒単位</summary>
    public float Cooldown { get; set; }

    /// <summary>突進による無敵時間秒単位</summary>
    public float InvincibleDuration { get; set; }

    /// <summary>突進開始時のストレッチ変形倍率</summary>
    public Vec2f StretchScale { get; set; }

    /// <summary>残像生成インターバル秒単位</summary>
    public float GhostTrailInterval { get; set; }

    /// <summary>残像のフェード持続時間秒単位</summary>
    public float GhostTrailDuration { get; set; }

    /// <summary>残像の初期アルファ値：0〜255</summary>
    public float GhostTrailAlpha { get; set; }
}
