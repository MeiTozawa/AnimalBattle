namespace AnimalBattle.Components;

/// <summary>
/// 弾幕 Boss のフェーズ（Phase 1/2/3）、各種攻撃タイマー、および浮動基準位置を管理するコンポーネント。
/// 体力・ダメージ管理は独立した <see cref="Health"/> コンポーネントに委譲されます。
/// </summary>
/// <remarks>
/// 指定された基準位置で <see cref="BossStats"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="baseX">浮動中心 X 座標</param>
/// <param name="baseY">浮動中心 Y 座標</param>
public struct BossStats(float baseX = 640.0f, float baseY = 180.0f)
{
    /// <summary>現在の戦闘フェーズ (1, 2, 3)</summary>
    public int Phase = 1;

    /// <summary>攻撃パターン 1 用のタイマー（秒）</summary>
    public float AttackTimer1 = 0.5f;

    /// <summary>攻撃パターン 2 用のタイマー（秒）</summary>
    public float AttackTimer2 = 1.0f;

    /// <summary>螺旋・回転弾幕の累積角度オフセット（ラジアン）</summary>
    public float AngleOffset = 0.0f;

    /// <summary>被弾時ヒットフラッシュ演出タイマー（秒）</summary>
    public float HitFlashTimer = 0.0f;

    /// <summary>浮動移動の基準 X 座標</summary>
    public float BaseX = baseX;

    /// <summary>浮動移動の基準 Y 座標</summary>
    public float BaseY = baseY;

    /// <summary>浮動アニメーションの経過時間（秒）</summary>
    public float HoverTimer = 0.0f;
}
