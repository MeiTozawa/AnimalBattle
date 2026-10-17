using System.Diagnostics;
using AnimalBattle.Core;

namespace AnimalBattle.Framework.Fsm;

/// <summary>
///     ゼロGC・アトミック遷移を提供するフラット有限状態マシン（FSM）。
/// </summary>
public class StateMachine : IState {
    private readonly List<IState> _states = [];
    private readonly List<(Func<bool> Condition, IState TargetState)> _globalTransitions = [];
    private IState? _currentState;

    /// <summary>状態マシン識別子を取得します。</summary>
    public StringId Id { get; }

    /// <summary>現在アクティブな状態を取得します。</summary>
    public IState? CurrentState => _currentState;

    /// <summary>
    ///     識別子を指定して状態マシンを初期化します。
    /// </summary>
    public StateMachine(StringId id)
    {
        Id = id;
    }

    /// <summary>状態を登録します。</summary>
    public void AddState(IState state)
    {
        Debug.Assert(state != null, "state は null であってはなりません。");
        if (state == null) return;
        _states.Add(state);
        _currentState ??= state;
    }

    /// <summary>互換性のための状態追加メソッド（AddState へ委譲）。</summary>
    public void AddSubState(IState state) => AddState(state);

    /// <summary>初期状態を明示的に設定します。</summary>
    public void SetInitialState(IState state)
    {
        Debug.Assert(state != null, "state は null であってはなりません。");
        if (state == null) return;
        Debug.Assert(_states.Contains(state), $"未登録の状態を初期状態に設定しようとしました: {state.Id}");
        _currentState = state;
    }

    /// <summary>最優先評価される大域遷移条件を追加します。</summary>
    public void AddGlobalTransition(Func<bool> condition, IState targetState)
    {
        Debug.Assert(condition != null && targetState != null, "condition または targetState は null であってはなりません。");
        if (condition == null || targetState == null) return;
        _globalTransitions.Add((condition, targetState));
    }

    /// <summary>指定状態へアトミックに切り替えます。</summary>
    public void ChangeState(IState targetState)
    {
        Debug.Assert(targetState != null, "targetState は null であってはなりません。");
        if (targetState == null || _currentState == targetState) return;

        _currentState?.Exit();
        _currentState = targetState;
        _currentState.Enter();
    }

    /// <inheritdoc />
    public void Enter()
    {
        _currentState?.Enter();
    }

    /// <inheritdoc />
    public void Update(float dt)
    {
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
    public void Exit()
    {
        _currentState?.Exit();
    }
}
