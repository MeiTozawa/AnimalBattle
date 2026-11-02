namespace AnimalBattle.Abilities.Attack;

using AnimalBattle.Skills;

/// <summary>
/// 設定から解決済みの攻撃タイムラインアクション（ステップ）を表す検証済み実行データ。
/// </summary>
public readonly struct AttackActionStep
{
    /// <summary>攻撃内で一意なアクションインデックスを取得します。</summary>
    public int Index { get; }

    /// <summary>Execute 開始からの発動遅延時間（秒）</summary>
    public float TriggerTime { get; }

    /// <summary>接地時駆動アクションかどうかを取得します。</summary>
    public bool IsOnGrounded { get; }

    /// <summary>設定駆動型エフェクト設定参照</summary>
    public SkillEffectConfig Effect { get; }

    /// <summary>
    /// 新規設定駆動型エフェクトに基づくアクションステップを生成します。
    /// </summary>
    public AttackActionStep(int index, SkillEffectConfig effect, float triggerTime, bool isOnGrounded)
    {
        Index = index;
        TriggerTime = triggerTime;
        IsOnGrounded = isOnGrounded;
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
