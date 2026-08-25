using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の自機狙い三方向弾 Execute State_Module です。</summary>
/// <remarks>Boss1 の設定と実行値を注入します。</remarks>
public sealed class Aimed3ShotExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state) : Boss1BarrageExecuteModule(configuration, state, Boss1AttackIds.Aimed3Shot)
{
    /// <inheritdoc />
    public override BossState Id => Boss1StateIds.Aimed3ShotExecute;

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        float dx = context.PlayerObservation.PositionX - State.AttackStartPosition.X;
        float dy = context.PlayerObservation.PositionY - State.AttackStartPosition.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.0001f)
        {
            dx = 1.0f;
            dy = 0.0f;
            length = 1.0f;
        }

        float baseAngle = MathF.Atan2(dy, dx);
        for (int index = -1; index <= 1; index++)
        {
            float angle = baseAngle + index * Pattern.AngleStep;
            if (!context.Attack.EmitBullet(Pattern.AttackId, MathF.Cos(angle) * Pattern.BulletSpeed, MathF.Sin(angle) * Pattern.BulletSpeed))
            {
                Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Aimed3Shot の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
            }
        }
    }
}
