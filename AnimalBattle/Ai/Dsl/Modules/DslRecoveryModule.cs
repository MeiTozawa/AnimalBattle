using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃後隙（Recovery）状態モジュールです。
/// </summary>
public sealed class DslRecoveryModule : IBossStateModule
{
    private readonly BossState _stateId;
    private readonly BossState _nextStateId;
    private readonly AttackId _attackId;
    private readonly float _duration;
    private float _elapsed;

    /// <inheritdoc />
    public BossState Id => _stateId;

    /// <summary>
    /// 後隙状態モジュールを初期化します。
    /// </summary>
    /// <param name="stateId">この後隙状態の識別子。</param>
    /// <param name="nextStateId">後隙完了後に遷移する次の状態の識別子。</param>
    /// <param name="attackId">対象の攻撃識別子。</param>
    /// <param name="duration">後隙持続時間（秒）。</param>
    public DslRecoveryModule(BossState stateId, BossState nextStateId, AttackId attackId, float duration)
    {
        _stateId = stateId;
        _nextStateId = nextStateId;
        _attackId = attackId;
        _duration = duration;
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed += context.DeltaTime;
        if (_elapsed >= _duration)
        {
            // 次の状態へ遷移
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
    }
}
