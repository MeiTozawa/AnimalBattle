namespace AnimalBattle.Abilities.Attack;

using AnimalBattle.Skills;

/// <summary>
/// 設定から解決済みの攻撃タイムラインアクション（ステップ）を表す検証済み実行データ。
/// </summary>
public readonly struct AttackActionStep
{
    /// <summary>アクションの種類を取得します。</summary>
    public AttackActionKind Kind { get; }

    /// <summary>攻撃内で一意なアクションインデックスを取得します。</summary>
    public int Index { get; }

    /// <summary>主要パラメータ 0（X 座標オフセット / 弾速 等）</summary>
    public float Param0 { get; }

    /// <summary>主要パラメータ 1（Y 座標オフセット / 弾数 等）</summary>
    public float Param1 { get; }

    /// <summary>補助パラメータ 2（拡散角度 / 振動強度 等）</summary>
    public float Param2 { get; }

    /// <summary>Execute 開始からの発動遅延時間（秒）</summary>
    public float TriggerTime { get; }

    /// <summary>設定駆動型エフェクト設定参照</summary>
    public SkillEffectConfig? Effect { get; }

    /// <summary>
    /// 新規設定駆動型エフェクトに基づくアクションステップを生成します。
    /// </summary>
    public AttackActionStep(int index, SkillEffectConfig effect, float triggerTime, bool isOnGrounded)
    {
        Kind = isOnGrounded ? AttackActionKind.OnGroundedHitbox : (effect.Kind switch
        {
            SkillEffectKind.Bullet => AttackActionKind.Bullet,
            SkillEffectKind.Hitbox => AttackActionKind.Hitbox,
            SkillEffectKind.Velocity => AttackActionKind.Movement,
            SkillEffectKind.ScreenShake => AttackActionKind.ScreenShake,
            _ => AttackActionKind.Movement
        });
        Index = index;
        Param0 = effect.Kind == SkillEffectKind.Velocity ? effect.Velocity.X : 0.0f;
        Param1 = effect.Kind == SkillEffectKind.Velocity ? effect.Velocity.Y : 0.0f;
        Param2 = effect.Kind == SkillEffectKind.ScreenShake ? effect.ShakeIntensity : 0.0f;
        TriggerTime = triggerTime;
        Effect = effect;
    }

    /// <summary>アクションの設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid =>
        Index >= 0 &&
        TriggerTime >= 0.0f &&
        IsFinite(TriggerTime) &&
        Effect != null;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
