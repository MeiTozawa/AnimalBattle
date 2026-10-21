namespace AnimalBattle.Player.Components;

/// <summary>
///     無敵点滅状態追跡コンポーネント。
///     毎フレームの可視性強制上書きを防ぎ、無敵終了時の単一エッジ復元を制御します。
/// </summary>
public struct InvincibleBlinkState
{
    /// <summary>前フレームにおいて無敵点滅中であったかどうかのフラグ。</summary>
    public bool IsBlinking;
}
