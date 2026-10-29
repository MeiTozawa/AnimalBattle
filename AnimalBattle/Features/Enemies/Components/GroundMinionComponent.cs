namespace AnimalBattle.Enemies.Components;

using AnimalBattle.Maps.Navigation;

/// <summary>
/// 地面近接追撃怪のナビゲーションプランおよび跳躍状態を管理するコンポーネント。
/// </summary>
public struct GroundMinionComponent
{
    /// <summary>キャッシュされたナビゲーションプラン。</summary>
    public NavPlan? CachedPlan;

    /// <summary>現在実行中の跳躍または下落リンク。</summary>
    public PlatformLink? ActiveLink;

    /// <summary>経路再探索のクールダウン秒数。</summary>
    public float RepathCooldown;

    /// <summary>跳躍実行のクールダウン秒数。</summary>
    public float JumpCooldown;

    /// <summary>前回経路探索時のプレイヤー座標。</summary>
    public Vec2f LastPlayerPos;

    /// <summary>
    /// 地面近接追撃怪コンポーネントを初期化します。
    /// </summary>
    public GroundMinionComponent()
    {
        CachedPlan = null;
        ActiveLink = null;
        RepathCooldown = 0.0f;
        JumpCooldown = 0.0f;
        LastPlayerPos = Vec2f.Zero;
    }
}
