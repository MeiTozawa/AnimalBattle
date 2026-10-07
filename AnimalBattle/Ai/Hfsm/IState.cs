using AnimalBattle.Core;

namespace AnimalBattle.Ai.Hfsm;

/// <summary>
///     階層状態マシン内の単一ステート契約です。
/// </summary>
public interface IState {
    /// <summary>状態識別子を取得します。</summary>
    StringId Id { get; }

    /// <summary>親状態を取得または設定します。</summary>
    IState? Parent { get; set; }

    /// <summary>状態突入時の処理を実行します。</summary>
    void Enter();

    /// <summary>毎フレームの更新処理を実行します。</summary>
    void Update(float dt);

    /// <summary>状態離脱時の処理を実行します。</summary>
    void Exit();
}
