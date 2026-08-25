using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の全方向リング弾 Execute State_Module です。</summary>
public sealed class RingBurstExecuteModule : Boss1BarrageExecuteModule
{
    /// <inheritdoc />
    public override StateId Id => Boss1StateIds.RingBurstExecute;

    /// <summary>Boss1 の設定と実行値を注入します。</summary>
    public RingBurstExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
        : base(configuration, state, Boss1AttackIds.RingBurst)
    {
    }

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        int count = Math.Max(4, Pattern.BulletCount);
        for (int index = 0; index < count; index++)
        {
            float angle = index / (float)count * MathF.Tau + State.AngleOffset;
            if (!context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed))
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RingBurst の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
            }
        }

        State.AngleOffset += Pattern.AngleStep;
    }
}
