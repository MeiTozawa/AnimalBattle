namespace AnimalBattle.Abilities.Attack;

/// <summary>設定から解決済みの攻撃タイムラインアクション（ステップ）を表す検証済み初期化データです。</summary>
/// <remarks>攻撃アクションステップを生成します。</remarks>
public readonly struct AttackActionStep(
    AttackActionKind kind,
    int index,
    float param0 = 0.0f,
    float param1 = 0.0f,
    float param2 = 0.0f,
    float triggerTime = 0.0f) {
    /// <summary>アクションの種類を取得します。</summary>
    public AttackActionKind Kind { get; } = kind;

    /// <summary>攻撃内で一意なアクション index を取得します。</summary>
    public int Index { get; } = index;

    /// <summary>主要パラメータ 0（X座標オフセット / 目標高度 / 弾速 等）を取得します。</summary>
    public float Param0 { get; } = param0;

    /// <summary>主要パラメータ 1（Y座標オフセット / 所要時間 / 弾数 等）を取得します。</summary>
    public float Param1 { get; } = param1;

    /// <summary>補助パラメータ 2（滞空時間 / 拡散角度 / 振動強度 等）を取得します。</summary>
    public float Param2 { get; } = param2;

    /// <summary>Execute 開始からの発動遅延時間（秒）を取得します。</summary>
    public float TriggerTime { get; } = triggerTime;

    /// <summary>アクションの設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => (int)Kind >= 1 && (int)Kind <= Enum.GetValues<AttackActionKind>().Length
     && Index >= 0
     && IsFinite(Param0)
     && IsFinite(Param1)
     && IsFinite(Param2)
     && TriggerTime >= 0.0f
     && IsFinite(TriggerTime);

    /// <summary>移動アクションステップを生成します。</summary>
    public static AttackActionStep CreateMovement(int index, float vx, float vy, float triggerTime) =>
        new(AttackActionKind.Movement, index, vx, vy, 0.0f, triggerTime);

    /// <summary>攻撃判定アクションステップを生成します。</summary>
    public static AttackActionStep CreateHitbox(int index, float ox, float oy, float triggerTime, bool isOnGrounded = false) =>
        new(isOnGrounded ? AttackActionKind.OnGroundedHitbox : AttackActionKind.Hitbox, index, ox, oy, 0.0f, triggerTime);

    /// <summary>弾幕発射アクションステップを生成します。</summary>
    public static AttackActionStep CreateBullet(int index, float speed, float count, float spread, float triggerTime) =>
        new(AttackActionKind.Bullet, index, speed, count, spread, triggerTime);

    /// <summary>音効再生アクションステップを生成します。</summary>
    public static AttackActionStep CreateAudio(int index, int soundId, float volume, float triggerTime) =>
        new(AttackActionKind.AudioCue, index, soundId, volume, 0.0f, triggerTime);

    /// <summary>画面振動アクションステップを生成します。</summary>
    public static AttackActionStep CreateScreenShake(int index, float intensity, float duration, float triggerTime) =>
        new(AttackActionKind.ScreenShake, index, intensity, duration, 0.0f, triggerTime);

    /// <summary>イージング移動アクションステップを生成します。</summary>
    public static AttackActionStep CreateEasingMovement(int index, float peakY, float duration, float triggerTime) =>
        new(AttackActionKind.EasingMovement, index, peakY, duration, 0.0f, triggerTime);

    /// <summary>急降下アクションステップを生成します。</summary>
    public static AttackActionStep CreateVelocityDive(int index, float speedY, float triggerTime) =>
        new(AttackActionKind.VelocityDive, index, 0.0f, speedY, 0.0f, triggerTime);

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
