namespace AnimalBattle.Framework.Fsm;

/// <summary>
///     状態マシン状態の基本実装クラスです。
/// </summary>
public class State : IState {
    /// <summary>状態識別子を取得します。</summary>
    public StringId Id { get; }

    /// <summary>突入時コールバック。</summary>
    protected readonly Action? OnEnterAction;

    /// <summary>
    ///     状態インスタンスを生成します。
    /// </summary>
    public State(StringId id, Action? onEnter = null)
    {
        Id = id;
        OnEnterAction = onEnter;
    }

    /// <inheritdoc />
    public virtual void Enter() => OnEnterAction?.Invoke();

    /// <inheritdoc />
    public virtual void Update(float dt) { }

    /// <inheritdoc />
    public virtual void Exit() { }
}
