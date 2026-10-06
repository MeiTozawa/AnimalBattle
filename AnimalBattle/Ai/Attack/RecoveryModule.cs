using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃生成と次遷移評価を終了条件まで抑止する共通 Recovery State_Module です。</summary>
public sealed class RecoveryModule : IBossStateModule {
    /// <summary>攻撃定義と State_Id を指定して Recovery を生成します。</summary>
    public RecoveryModule(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        HandledState = stateId;
        Definition = definition;
    }

    /// <summary>現在の技能識別子を取得します。</summary>
    public BossSkill Skill => Definition.Skill;

    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition { get; private set; }

    /// <summary>Recovery が現在アクティブかどうかを取得します。</summary>
    public bool IsActive { get; private set; }

    /// <summary>Recovery 完了後に次遷移を評価できるかどうかを取得します。</summary>
    public bool CanEvaluateNextTransition => !IsActive && Elapsed > 0;

    /// <summary>Recovery の経過時間を取得します。</summary>
    public float Elapsed { get; private set; }

    /// <summary>Recovery の担当状態を取得します。</summary>
    public BossState HandledState { get; private set; }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        Start(ref context, in invocation);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!IsActive) return;

        Elapsed = MathF.Min(Definition.RecoveryDuration, Elapsed + context.DeltaTime);
        if (Elapsed >= Definition.RecoveryDuration) IsActive = false;
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        IsActive = false;
    }

    /// <summary>
    ///     既存インスタンスを再利用するために、状態識別子と攻撃定義を就地リセットします。
    /// </summary>
    public void Reset(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        HandledState = stateId;
        Definition = definition;
        IsActive = false;
        Elapsed = 0;
    }

    /// <summary>Recovery を開始します。開始に失敗した場合は安全な非攻撃行動を選択します。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (IsActive) return Result.Success;

        if (!Definition.IsValid || !invocation.IsValid)
        {
            return Error.Validation("RecoveryModule.Start", "攻撃定義またはフレーム呼び出しが不正です。");
        }

        Elapsed = 0;
        IsActive = true;
        return Result.Success;
    }
}