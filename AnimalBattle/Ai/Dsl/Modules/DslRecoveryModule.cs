using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃後隙（Recovery）状態モジュールです。
/// </summary>
public sealed class DslRecoveryModule : IBossStateModule
{
    private readonly BossState _handledState;
    private readonly BossState _nextState;
    private readonly BossSkill _skill;
    private readonly float _duration;
    private float _elapsed;

    /// <inheritdoc />
    public BossState HandledState => _handledState;

    /// <summary>
    /// 後隙状態モジュールを初期化します。
    /// </summary>
    /// <param name="handledState">この後隙状態の識別値。</param>
    /// <param name="nextState">後隙完了後に遷移する次の状態の識別値。</param>
    /// <param name="skill">対象の技能識別子。</param>
    /// <param name="duration">後隙持続時間（秒）。</param>
    public DslRecoveryModule(BossState handledState, BossState nextState, BossSkill skill, float duration)
    {
        _handledState = handledState;
        _nextState = nextState;
        _skill = skill;
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
