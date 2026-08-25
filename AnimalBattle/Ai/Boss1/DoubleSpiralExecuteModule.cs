using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の双螺旋弾 Execute State_Module です。</summary>
public sealed class DoubleSpiralExecuteModule : Boss1BarrageExecuteModule
{
    /// <inheritdoc />
    public override BossState Id => Boss1StateIds.DoubleSpiralExecute;

    /// <summary>Boss1 の設定と実行値を注入します。</summary>
    public DoubleSpiralExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
        : base(configuration, state, Boss1AttackIds.DoubleSpiral)
    {
    }

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        float first = State.AngleOffset;
        float second = first + MathF.PI;
        if (!context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(first) * Pattern.BulletSpeed, MathF.Sin(first) * Pattern.BulletSpeed)
            || !context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(second) * Pattern.BulletSpeed, MathF.Sin(second) * Pattern.BulletSpeed))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DoubleSpiral の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }

        State.AngleOffset += Pattern.AngleStep;
    }
}
