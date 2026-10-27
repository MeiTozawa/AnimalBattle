namespace AnimalBattle.Framework.Fsm;

/// <summary>
///     有限状態マシン状態の汎用インターフェース契約。
/// </summary>
public interface IState {
    /// <summary>状態識別子を取得します。</summary>
    StringId Id { get; }

    /// <summary>状態突入処理を実行します。</summary>
    void Enter();

    /// <summary>毎フレームの状態論理を更新します。</summary>
    void Update(float dt);

    /// <summary>状態離脱処理を実行します。</summary>
    void Exit();
}
