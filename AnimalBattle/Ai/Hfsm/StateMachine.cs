using System.Diagnostics;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Hfsm;

/// <summary>
///     親状態と子状態の階層更新およびグローバル優先遷移を管理する階層状態マシンです。
/// </summary>
public class StateMachine : State {
    private readonly List<IState> _subStates = [];
    private readonly List<(Func<bool> Condition, IState TargetState)> _globalTransitions = [];
    private IState? _currentState;

    /// <summary>この状態マシンがアクティブであるか（ルートマシン、または親状態マシンで現在選択されているか）を取得します。</summary>
    public bool IsActive => Parent switch
    {
        null => true,
        StateMachine parentSm => parentSm.IsActive && parentSm.CurrentState == this,
        _ => true
    };

    /// <summary>現在アクティブな子状態を取得します。</summary>
    public IState? CurrentState => _currentState;

    /// <summary>
    ///     状態マシンインスタンスを生成します。
    /// </summary>
    public StateMachine(StringId id) : base(id) { }

    /// <summary>子状態を追加します。</summary>
    public void AddSubState(IState state)
    {
        Debug.Assert(state != null, "state は null であってはなりません。");
        if (state == null) return;
        state.Parent = this;
        _subStates.Add(state);
        _currentState ??= state;
    }

    /// <summary>初期状態を明示的に設定します。</summary>
    public void SetInitialState(IState state)
    {
        Debug.Assert(state != null, "state は null であってはなりません。");
        if (state == null) return;
        Debug.Assert(_subStates.Contains(state), $"未登録の状態を初期状態に設定しようとしました: {state.Id}");
        _currentState = state;
    }

    /// <summary>最優先評価されるグローバル遷移条件を追加します。</summary>
    public void AddGlobalTransition(Func<bool> condition, IState targetState)
    {
        Debug.Assert(condition != null && targetState != null, "condition または targetState は null であってはなりません。");
        if (condition == null || targetState == null) return;
        _globalTransitions.Add((condition, targetState));
    }

    /// <summary>指定子状態へ切り替えます。</summary>
    public void ChangeState(IState targetState)
    {
        Debug.Assert(targetState != null, "targetState は null であってはなりません。");
        if (targetState == null || _currentState == targetState) return;

        if (IsActive)
        {
            _currentState?.Exit();
            _currentState = targetState;
            _currentState.Enter();
        }
        else
        {
            _currentState = targetState;
        }
    }

    /// <summary>識別子を指定して子状態を検索します。</summary>
    public bool TryGetSubState(StringId id, out IState? state)
    {
        for (var i = 0; i < _subStates.Count; i++)
        {
            if (_subStates[i].Id == id)
            {
                state = _subStates[i];
                return true;
            }
        }

        state = null;
        return false;
    }

    /// <inheritdoc />
    public override void Enter()
    {
        base.Enter();
        _currentState?.Enter();
    }

    /// <inheritdoc />
    public override void Update(float dt)
    {
        base.Update(dt);

        for (var i = 0; i < _globalTransitions.Count; i++)
        {
            var (condition, target) = _globalTransitions[i];
            if (condition() && _currentState != target)
            {
                ChangeState(target);
                return;
            }
        }

        _currentState?.Update(dt);
    }

    /// <inheritdoc />
    public override void Exit()
    {
        _currentState?.Exit();
        base.Exit();
    }
}
