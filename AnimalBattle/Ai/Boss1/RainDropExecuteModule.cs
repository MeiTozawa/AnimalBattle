using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の降雨弾 Execute State_Module です。</summary>
public sealed class RainDropExecuteModule : Boss1BarrageExecuteModule
{
    /// <inheritdoc />
    public override BossState Id => Boss1StateIds.RainDropExecute;

    /// <summary>Boss1 の設定と実行値を注入します。</summary>
    public RainDropExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
        : base(configuration, state, Boss1AttackIds.RainDrop)
    {
    }

    /// <inheritdoc />
    protected override void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex)
    {
        float range = Pattern.MaxX - Pattern.MinX;
        float firstX = Pattern.MinX + context.Random.NextSingle() * range;
        float secondX = Pattern.MinX + context.Random.NextSingle() * range;
        bool firstAccepted = context.Attack.EmitBullet(Pattern.AttackId, firstX, Pattern.BulletSpeed);
        bool secondAccepted = context.Attack.EmitBullet(Pattern.AttackId, secondX, Pattern.BulletSpeed);
        if (!firstAccepted || !secondAccepted)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "RainDrop の弾発行に失敗しました。", Pattern.AttackId, AttackStage.Execute);
        }
    }
}
