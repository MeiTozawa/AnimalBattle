namespace AnimalBattle.Player.Components;

/// <summary>
///     被弾時に付与される無敵保護時間の設定コンポーネント。
/// </summary>
public struct InvincibleOnHit(float duration) {
    /// <summary>被弾無敵持続時間（秒）。</summary>
    public float Duration = duration;
}
