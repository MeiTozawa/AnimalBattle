using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>足場跨ぎ機動の初期進入モードです。</summary>
public enum TraverseMode {
    /// <summary>能動的な跳躍上昇から開始します。</summary>
    Jump,

    /// <summary>足場からの落下・踏み外しから開始します。</summary>
    Fall,

    /// <summary>着地硬直のみを実行します。</summary>
    Landing
}

/// <summary>機動モジュール内部の進行段階です。</summary>
public enum TraverseStage {
    /// <summary>上昇段階です。</summary>
    Ascent,

    /// <summary>下降段階です。</summary>
    Descent,

    /// <summary>着地後の硬直段階です。</summary>
    Landing
}

/// <summary>
///     Boss2 の足場間跳躍、空中落下、および着地硬直の生命周期を一元処理する機動モジュールです。
/// </summary>
/// <param name="stateId">対象ステート。</param>
/// <param name="initialMode">初期モード。</param>
/// <param name="configuration">設定境界。</param>
/// <param name="state">共有状態。</param>
public sealed class Boss2PlatformTraverseModule(
    BossState stateId,
    TraverseMode initialMode,
    IBoss2Configuration configuration,
    Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider {
    private readonly IBoss2Configuration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly TraverseMode _initialMode = initialMode;
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private float _stageElapsed;

    /// <summary>現在の内部進行段階を取得します。</summary>
    public TraverseStage CurrentStage { get; private set; }

    /// <inheritdoc />
    public BossState HandledState { get; } = stateId;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _stageElapsed = 0.0f;

        switch (_initialMode)
        {
            case TraverseMode.Jump: {
                var settings = _configuration.GetPhase(context.Phase);
                _state.IsGrounded = false;
                var jumpVy = _state.CurrentVelocity.Y < 0.0f ? _state.CurrentVelocity.Y : -settings.JumpForce;
                _state.CurrentVelocity = new Vec2f(_state.CurrentVelocity.X, jumpVy);
                context.Movement.SetVelocity(_state.CurrentVelocity.X, jumpVy);
                CurrentStage = TraverseStage.Ascent;
                break;
            }

            case TraverseMode.Fall: {
                _state.IsGrounded = false;
                CurrentStage = TraverseStage.Descent;
                break;
            }

            case TraverseMode.Landing: {
                context.Movement.Stop();
                context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
                CurrentStage = TraverseStage.Landing;
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead) return;

        _state.StateElapsed += context.DeltaTime;
        _stageElapsed += context.DeltaTime;
        var settings = _configuration.GetPhase(context.Phase);

        switch (CurrentStage)
        {
            case TraverseStage.Ascent: {
                context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
                if (_state.CurrentVelocity.Y >= 0.0f || _stageElapsed >= settings.MaxAscentDuration)
                {
                    CurrentStage = TraverseStage.Descent;
                    _stageElapsed = 0.0f;
                }

                break;
            }

            case TraverseStage.Descent: {
                context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
                if (_state.IsGrounded && _stageElapsed >= 0.05f)
                {
                    CurrentStage = TraverseStage.Landing;
                    _stageElapsed = 0.0f;
                    context.Movement.Stop();
                    context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
                }

                break;
            }

            case TraverseStage.Landing: {
                context.Movement.Stop();
                if (_stageElapsed >= settings.LandingRecoveryDuration)
                    _state.RequestTransition(Boss2State.PatrolChase, new TransitionReason(ReasonKind.TimerElapsed));
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _stageElapsed = 0.0f;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "EffectPort"];
}