using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;


/// <summary>
/// Boss 2: 泰山圧頂（LeapSmash）の上昇・飛翔 Execute State_Module です。
/// 地上からプレイヤーの頭上空中へ向けて高速飛翔し、最高点で 1.0 秒間ホバリング滞空（懸停）します。
/// </summary>
/// <remarks>Boss2 設定と共有実行値を注入します。</remarks>
public sealed class Boss2LeapSmashAscentModule(IBoss2Configuration configuration, Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private float _startX;
    private float _startY;
    private bool _isHoveringAtPeak;
    private float _hoverTimer;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.LeapSmashAscent;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "ObservationService", "PlayerObservation"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = true;
        _isHoveringAtPeak = false;
        _hoverTimer = 0.0f;

        _startX = _state.CurrentPosition.X;
        _startY = _state.CurrentPosition.Y;

        LeapSmashConfig leapCfg = ConfigManager.Pattern.LeapSmash;
        float targetX = context.PlayerObservation.PositionX;
        _state.AttackTargetPosition = new Vec2f(targetX, leapCfg.PeakAltitudeY);
        _state.FacingRight = targetX >= _startX;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        _state.StateElapsed += context.DeltaTime;
        LeapSmashConfig leapCfg = ConfigManager.Pattern.LeapSmash;
        float flyDuration = leapCfg.FlyDuration > 0.0f ? leapCfg.FlyDuration : 0.45f;
        float peakY = leapCfg.PeakAltitudeY > 0.0f ? leapCfg.PeakAltitudeY : 130.0f;
        float apexHangDuration = leapCfg.ApexHangDuration > 0.0f ? leapCfg.ApexHangDuration : 1.00f;

        if (!_isHoveringAtPeak)
        {
            float t = flyDuration > 0.0f ? Math.Clamp(_state.StateElapsed / flyDuration, 0.0f, 1.0f) : 1.0f;
            // 滑らかなイージング（Sine イーズアウト）でプレイヤー頭上へ上昇飛翔
            float easeT = MathF.Sin(t * MathF.PI * 0.5f);

            // プレイヤーの最新位置に向けて頭上目標 X を追随
            float targetX = context.PlayerObservation.PositionX;
            float currentX = _startX + (targetX - _startX) * easeT;
            float currentY = _startY + (peakY - _startY) * easeT;

            _state.FacingRight = targetX >= _state.CurrentPosition.X;
            context.Movement.MoveTo(currentX, currentY);

            // 設定された飛翔所要時間を超えたら最高点滞空（停顿）フェーズへ
            if (_state.StateElapsed >= flyDuration)
            {
                _isHoveringAtPeak = true;
                _hoverTimer = 0.0f;
                _state.AttackTargetPosition = new Vec2f(targetX, peakY);
            }
        }

        else
        {
            // 最高点頭上懸停（停顿）中：空中で完全静止し、プレイヤーに回避・反応時間を与える
            _hoverTimer += context.DeltaTime;
            context.Movement.MoveTo(_state.AttackTargetPosition.X, peakY);

            // 懸停完了後、垂直急降下叩きつけ（Descent）へ移行
            if (_hoverTimer >= apexHangDuration)
            {
                _state.RequestTransition(Boss2StateIds.LeapSmashDescent, new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
            }
        }
    }


    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _isHoveringAtPeak = false;
        _hoverTimer = 0.0f;
    }
}

