using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     Boss AI の一状態が実装する共通ライフサイクル契約です。
/// </summary>
public interface IBossStateModule {
    /// <summary>このモジュールが担当・処理する状態を取得します。</summary>
    BossState HandledState { get; }

    /// <summary>状態を一意に識別する公開プロパティです。</summary>
    BossState State => HandledState;

    /// <summary>状態へ入場します。</summary>
    void Enter(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>状態を一回更新します。</summary>
    void Update(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>状態から退場します。</summary>
    void Exit(ref StateContext context, in LifecycleInvocation invocation);
}

/// <summary>
///     State_Module が必要とする名前付き依存関係を宣言する任意の契約です。
/// </summary>
public interface IStateModuleDependencyProvider {
    /// <summary>必須依存関係の名前を取得します。</summary>
    IReadOnlyList<string> RequiredDependencies { get; }
}