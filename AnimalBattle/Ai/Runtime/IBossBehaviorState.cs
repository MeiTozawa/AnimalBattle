using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// State_Module 間で共有する状態遷移要求および位置同期を提供するインターフェースです。
/// </summary>
public interface IBossBehaviorState
{
    /// <summary>保持済みの遷移要求を読み取り、消費します。</summary>
    /// <param name="stateId">次に要求された状態 ID。</param>
    /// <param name="reason">遷移理由。</param>
    /// <returns>有効な遷移要求が存在した場合は true。</returns>
    bool ConsumeTransition(out BossState stateId, out TransitionReason reason);

    /// <summary>現在の Boss 実体位置を同期します。</summary>
    /// <param name="x">Boss の X 座標。</param>
    /// <param name="y">Boss の Y 座標。</param>
    void SynchronizePosition(float x, float y);

    /// <summary>現在の Boss 実体位置、速度、接地状態およびプレイヤー生存状態を同期します。</summary>
    /// <param name="x">Boss の X 座標。</param>
    /// <param name="y">Boss の Y 座標。</param>
    /// <param name="vx">Boss の X 速度。</param>
    /// <param name="vy">Boss の Y 速度。</param>
    /// <param name="isGrounded">Boss が足場または地面に接地しているかどうか。</param>
    /// <param name="isPlayerAlive">プレイヤーが生存しているかどうか。</param>
    void SynchronizeState(float x, float y, float vx, float vy, bool isGrounded, bool isPlayerAlive);

    /// <summary>現在 Boss が右を向いているかどうかを取得します。</summary>
    bool FacingRight { get; }
}
