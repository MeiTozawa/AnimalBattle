using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>足場跨ぎ機動の初期進入モードです。</summary>
public enum TraverseMode
{
    /// <summary>能動的な跳躍上昇から開始します。</summary>
    Jump,
    /// <summary>足場からの落下・踏み外しから開始します。</summary>
    Fall,
    /// <summary>着地硬直のみを実行します。</summary>
    Landing
}

/// <summary>機動モジュール内部の進行段階です。</summary>
public enum TraverseStage
{
    /// <summary>上昇段階です。</summary>
    Ascent,
    /// <summary>下降段階です。</summary>
    Descent,
    /// <summary>着地後の硬直段階です。</summary>
    Landing
}

/// <summary>
/// Boss2 の足場間跳躍、空中落下、および着地硬直の生命周期を一元処理する機動モジュールです。
/// </summary>
/// <param name="stateId">対象ステート。</param>
/// <param name="initialMode">初期モード。</param>
/// <param name="configuration">設定境界。</param>
/// <param name="state">共有状態。</param>
public sealed class Boss2PlatformTraverseModule(
    BossState stateId,
    TraverseMode initialMode,
    IBoss2Configuration configuration,
    Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly BossState _stateId = stateId;
    private readonly TraverseMode _initialMode = initialMode;
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private TraverseStage _stage;
    private float _stageElapsed;

    /// <inheritdoc />
    public BossState HandledState => _stateId;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "EffectPort"];
    /// <summary>現在の内部進行段階を取得します。</summary>
    public TraverseStage CurrentStage => _stage;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _stageElapsed = 0.0f;

        switch (_initialMode)
        {
            case TraverseMode.Jump:
                {
                    Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
                    _state.IsGrounded = false;
                    float jumpVy = _state.CurrentVelocity.Y < 0.0f ? _state.CurrentVelocity.Y : -settings.JumpForce;
                    _state.CurrentVelocity = new(_state.CurrentVelocity.X, jumpVy);
                    context.Movement.SetVelocity(_state.CurrentVelocity.X, jumpVy);
                    _stage = TraverseStage.Ascent;
                    break;
                }

            case TraverseMode.Fall:
                {
                    _state.IsGrounded = false;
                    _stage = TraverseStage.Descent;
                    break;
                }

            case TraverseMode.Landing:
                {
                    context.Movement.Stop();
                    context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
                    _stage = TraverseStage.Landing;
                    break;
                }
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        _state.StateElapsed += context.DeltaTime;
        _stageElapsed += context.DeltaTime;
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);

        switch (_stage)
        {
            case TraverseStage.Ascent:
                {
                    context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
                    if (_state.CurrentVelocity.Y >= 0.0f || _stageElapsed >= settings.MaxAscentDuration)
                    {
                        _stage = TraverseStage.Descent;
                        _stageElapsed = 0.0f;
                    }
                    break;
                }

            case TraverseStage.Descent:
                {
                    context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);
                    if (_state.IsGrounded && _stageElapsed >= 0.05f)
                    {
                        _stage = TraverseStage.Landing;
                        _stageElapsed = 0.0f;
                        context.Movement.Stop();
                        context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
                    }
                    break;
                }

            case TraverseStage.Landing:
                {
                    context.Movement.Stop();
                    if (_stageElapsed >= settings.LandingRecoveryDuration)
                    {
                        _state.RequestTransition(Boss2State.PatrolChase, new TransitionReason(ReasonKind.TimerElapsed));
                    }
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
}
