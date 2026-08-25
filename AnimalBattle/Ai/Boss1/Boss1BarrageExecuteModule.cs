using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の弾幕 Execute module が共有する共通 lifecycle 実装です。</summary>
public abstract class Boss1BarrageExecuteModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private ExecuteGate? _gate;
    private float _elapsed;
    private float _nextEmission;
    private int _emissionIndex;

    /// <summary>設定を取得します。</summary>
    protected IBoss1Configuration Configuration => _configuration;
    /// <summary>共有 Boss1 実行値を取得します。</summary>
    protected Boss1BehaviorState State => _state;
    /// <summary>現在の攻撃 pattern を取得します。</summary>
    protected Boss1AttackPattern Pattern { get; private set; }
    /// <inheritdoc />
    public abstract StateId Id { get; }
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "AttackPort", "MovementPort", "RandomSource"];

    /// <summary>指定された Boss1 攻撃の Execute module を生成します。</summary>
    protected Boss1BarrageExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state, AttackId attackId)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (!configuration.TryGetAttack(attackId, out Boss1AttackPattern pattern))
        {
            pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.Aimed3Shot, 0, 0, 0);
        }

        Pattern = pattern;
        _gate = new ExecuteGate(pattern.Definition);
    }

    /// <inheritdoc />
    public virtual void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _nextEmission = 0.0f;
        _emissionIndex = 0;
        _state.StateElapsed = 0.0f;
        _state.AttackSubTimer = 0.0f;
        _gate!.Reset();
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.CurrentAttackId != Pattern.AttackId)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidAttackId, "Boss1 の Execute 対象が現在攻撃と一致しません。", Pattern.AttackId, AttackStage.Execute);
        }
    }

    /// <inheritdoc />
    public virtual void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead || _gate is null)
        {
            return;
        }

        ExecuteResult result = _gate.Execute(ref context, in invocation);
        if (!result.Started)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.SafetyFallback, attackId: Pattern.AttackId, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        float duration = Pattern.Definition.ExecuteDuration;
        float interval = Pattern.Interval;
        if (interval > 0 && _elapsed >= _nextEmission && _elapsed <= duration)
        {
            EmitDueComponents(ref context, in invocation);
            _nextEmission += interval;
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        _state.AttackSubTimer = MathF.Max(0.0f, _nextEmission - _elapsed);
        if (result.IsComplete || _elapsed >= duration)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, attackId: Pattern.AttackId));
        }
    }

    /// <inheritdoc />
    public virtual void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate?.Reset();
        _state.AttackSubTimer = 0.0f;
    }

    /// <summary>一回の発射時点に必要な Boss1 固有 component を port へ渡します。</summary>
    protected abstract void EmitComponents(ref StateContext context, in LifecycleInvocation invocation, int emissionIndex);

    private void EmitDueComponents(ref StateContext context, in LifecycleInvocation invocation)
    {
        EmitComponents(ref context, in invocation, _emissionIndex++);
    }
}
