namespace AnimalBattle.Ai.Attack;

/// <summary>設定から解決済みの攻撃 component を表す検証済み初期化データです。</summary>
/// <remarks>攻撃 component を生成します。</remarks>
public readonly struct AttackComponent(
    AttackComponentKind kind,
    int index,
    float param0 = 0.0f,
    float param1 = 0.0f,
    float param2 = 0.0f,
    float triggerTime = 0.0f) {
    /// <summary>component の種類を取得します。</summary>
    public AttackComponentKind Kind { get; } = kind;

    /// <summary>攻撃内で一意な component index を取得します。</summary>
    public int Index { get; } = index;

    /// <summary>主要パラメータ 0（X座標オフセット / 目標高度 / 弾速 等）を取得します。</summary>
    public float Param0 { get; } = param0;

    /// <summary>主要パラメータ 1（Y座標オフセット / 所要時間 / 弾数 等）を取得します。</summary>
    public float Param1 { get; } = param1;

    /// <summary>補助パラメータ 2（滞空時間 / 拡散角度 / 振動強度 等）を取得します。</summary>
    private float Param2 { get; } = param2;

    /// <summary>Execute 開始からの発動遅延時間（秒）を取得します。</summary>
    public float TriggerTime { get; } = triggerTime;

    /// <summary>component の設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => (int)Kind >= 1 && (int)Kind <= Enum.GetValues<AttackComponentKind>().Length
     && Index >= 0
     && IsFinite(Param0)
     && IsFinite(Param1)
     && IsFinite(Param2)
     && TriggerTime >= 0.0f
     && IsFinite(TriggerTime);

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}