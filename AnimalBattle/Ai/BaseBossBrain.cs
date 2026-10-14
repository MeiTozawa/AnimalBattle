using AnimalBattle.Ai.Hfsm;

namespace AnimalBattle.Ai;

/// <summary>
///     Boss 意思決定ブレインの共通基底抽象クラス。
///     階層型有限状態機械（HFSM）のルート・戦闘・反応状態マシン構造、
///     GAS アビリティシステム連携、フェーズ遷移判定、死亡制御を一元管理します。
/// </summary>
public abstract class BaseBossBrain : IBossBrain
{
    protected static readonly StringId StateRoot = "Root";
    protected static readonly StringId StateCombat = "Combat";
    protected static readonly StringId StateReaction = "Reaction";
    protected static readonly StringId StatePhaseTransition = "PhaseTransition";
    protected static readonly StringId StateDead = "Dead";

    protected readonly AbilitySystemComponent _asc = new();
    protected readonly StateMachine _rootMachine = new(StateRoot);
    protected readonly StateMachine _combatMachine = new(StateCombat);
    protected readonly StateMachine _reactionMachine = new(StateReaction);

    protected State _phaseTransitionState = null!;
    protected State _deadState = null!;

    protected float _stateTimer;
    protected bool _isDead;
    protected Phase? _pendingPhaseTransition;

    protected Vec2f _currentPosition;
    protected Vec2f _targetPosition;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; protected set; } = Phase.Phase1;

    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    public AbilitySystemComponent AbilitySystem => _asc;

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>AI 目標地点座標を取得します。</summary>
    public Vec2f TargetPosition => _targetPosition;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => _targetPosition.X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => _targetPosition.Y;

    /// <summary>現在発動中の技能識別タグを取得します。</summary>
    public virtual StringId CurrentSkillTag => _asc.CurrentAbility?.AbilityTag ?? StringId.Empty;

    /// <summary>現在 AI 状態の経過時間（秒）を取得します。</summary>
    public virtual float StateTimer => _stateTimer;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _isDead;

    /// <summary>Boss が右向きであるかを取得します（派生クラスでオーバーライド可能）。</summary>
    public virtual bool FacingRight => true;

    /// <summary>弾幕波次発射イベント。</summary>
    public event Action<StringId, Vec2f, Vec2f>? OnEmitBulletWave;

    /// <summary>全弾幕消去イベント。</summary>
    public event Action? OnClearBullets;

    /// <summary>現在の Boss 統一マクロ状態を取得します。</summary>
    public abstract BossState CurrentState { get; }


    /// <summary>
    ///     弾幕発射イベントを発火します。
    /// </summary>
    protected void RaiseEmitBulletWave(StringId skill, Vec2f origin, Vec2f target)
    {
        OnEmitBulletWave?.Invoke(skill, origin, target);
    }

    /// <summary>
    ///     弾幕消去イベントを発火します。
    /// </summary>
    protected void RaiseClearBullets()
    {
        OnClearBullets?.Invoke();
    }

    /// <summary>
    ///     共通状態マシン構造（Root, Combat, Reaction, PhaseTransition, Dead）を初期化します。
    /// </summary>
    protected void InitializeBaseStateMachine()
    {
        _phaseTransitionState = new State(StatePhaseTransition, onEnter: () =>
        {
            _stateTimer = 0.0f;
            _asc.InterruptCurrentAbility();
            _asc.AddTag(GameplayTag.Invincible);
            if (_pendingPhaseTransition.HasValue)
            {
                Phase = _pendingPhaseTransition.Value;
                _pendingPhaseTransition = null;
                OnPhaseTransitionEntered(Phase);
            }
        }, onExit: () =>
        {
            _asc.RemoveTag(GameplayTag.Invincible);
        });

        _deadState = new State(StateDead, onEnter: () =>
        {
            _asc.InterruptCurrentAbility();
            RaiseClearBullets();
            OnDeadEntered();
        });

        _reactionMachine.AddSubState(_phaseTransitionState);
        _reactionMachine.AddSubState(_deadState);

        _rootMachine.AddSubState(_combatMachine);
        _rootMachine.AddSubState(_reactionMachine);
        _rootMachine.SetInitialState(_combatMachine);

        _rootMachine.AddGlobalTransition(() => _isDead, _reactionMachine);
        _rootMachine.AddGlobalTransition(() => _pendingPhaseTransition.HasValue, _reactionMachine);
    }

    /// <summary>フェーズ遷移状態突入時の追加処理フック（派生クラスで拡張）。</summary>
    protected virtual void OnPhaseTransitionEntered(Phase newPhase) { }

    /// <summary>死亡状態突入時の追加処理フック（派生クラスで拡張）。</summary>
    protected virtual void OnDeadEntered() { }

    /// <summary>
    ///     死亡状態へ遷移します。
    /// </summary>
    public virtual void Kill()
    {
        _isDead = true;
        _reactionMachine.ChangeState(_deadState);
        _rootMachine.ChangeState(_reactionMachine);
    }

    /// <summary>
    ///     現在の HP 割合に基づいてフェーズ遷移を評価します。
    /// </summary>
    protected void EvaluatePhaseTransition(float currentHp, float maxHp, float p2Threshold, float p3Threshold)
    {
        if (maxHp <= 0.0f || _pendingPhaseTransition.HasValue) return;
        if (_rootMachine.CurrentState == _reactionMachine) return;

        var hpRatio = currentHp / maxHp;

        if (Phase == Phase.Phase1 && hpRatio <= p2Threshold)
        {
            _pendingPhaseTransition = Phase.Phase2;
            _reactionMachine.ChangeState(_phaseTransitionState);
            _rootMachine.ChangeState(_reactionMachine);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= p3Threshold)
        {
            _pendingPhaseTransition = Phase.Phase3;
            _reactionMachine.ChangeState(_phaseTransitionState);
            _rootMachine.ChangeState(_reactionMachine);
        }
    }
}
