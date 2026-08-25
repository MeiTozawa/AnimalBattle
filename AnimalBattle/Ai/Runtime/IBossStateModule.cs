using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// Boss AI の一状態が実装する共通ライフサイクル契約です。
/// </summary>
public interface IBossStateModule
{
    /// <summary>旧実装が返す固定状態識別子です。</summary>
    BossState Id { get; }

    /// <summary>状態を固定 enum で一意に識別します。</summary>
    BossState State => Id;

    /// <summary>状態へ入場します。</summary>
    void Enter(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>状態を一回更新します。</summary>
    void Update(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>状態から退場します。</summary>
    void Exit(ref StateContext context, in LifecycleInvocation invocation);
}

/// <summary>
/// State_Module が必要とする名前付き依存関係を宣言する任意の契約です。
/// </summary>
public interface IStateModuleDependencyProvider
{
    /// <summary>必須依存関係の名前を取得します。</summary>
    IReadOnlyList<string> RequiredDependencies { get; }
}
