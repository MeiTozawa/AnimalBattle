using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の高速狙撃弾 Execute State_Module です。</summary>
public sealed class FastSniperExecuteModule : Boss1BarrageExecuteModule
{
    /// <inheritdoc />
    public override BossState Id => Boss1StateIds.FastSniperExecute;

    /// <summary>Boss1 の設定と実行値を注入します。</summary>
    public FastSniperExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
        : base(configuration, state, Boss1AttackIds.FastSniper)
    {
    }

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        float dx = context.PlayerObservation.PositionX - State.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - State.AttackStartPosition.Y;
        float angle = MathF.Atan2(dy, dx);
        if (!context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "FastSniper の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }
    }
}
