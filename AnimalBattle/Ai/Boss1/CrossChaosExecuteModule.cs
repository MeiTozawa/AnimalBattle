using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の交差波弾 Execute State_Module です。</summary>
/// <remarks>Boss1 の設定と実行値を注入します。</remarks>
public sealed class CrossChaosExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state) : Boss1BarrageExecuteModule(configuration, state, Boss1AttackIds.CrossChaos)
{
    /// <inheritdoc />
    public override StateId Id => Boss1StateIds.CrossChaosExecute;

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        bool accepted = true;
        for (int index = 0; index < 4; index++)
        {
            float angle = State.AngleOffset + index * (MathF.PI / 2.0f);
            accepted &= context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed);
        }

        if (!accepted)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "CrossChaos の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }

        State.AngleOffset += Pattern.AngleStep;
    }
}
