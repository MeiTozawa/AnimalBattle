namespace AnimalBattle.Components;

/// <summary>
/// 弾幕 Boss の体力、フェーズ（Phase 1/2/3）、各種攻撃タイマー、および浮動基準位置を管理するコンポーネント。
/// </summary>
public struct BossStats
{
    /// <summary>最大体力値（各フェーズ 30 HP、合計 90 HP）</summary>
    public float MaxHp;

    /// <summary>現在の残り体力値</summary>
    public float CurrentHp;

    /// <summary>現在の戦闘フェーズ (1, 2, 3)</summary>
    public int Phase;

    /// <summary>攻撃パターン 1 用のタイマー（秒）</summary>
    public float AttackTimer1;

    /// <summary>攻撃パターン 2 用のタイマー（秒）</summary>
    public float AttackTimer2;

    /// <summary>螺旋・回転弾幕の累積角度オフセット（ラジアン）</summary>
    public float AngleOffset;

    /// <summary>被弾時ヒットフラッシュ演出タイマー（秒）</summary>
    public float HitFlashTimer;

    /// <summary>浮動移動の基準 X 座標</summary>
    public float BaseX;

    /// <summary>浮動移動の基準 Y 座標</summary>
    public float BaseY;

    /// <summary>浮動アニメーションの経過時間（秒）</summary>
    public float HoverTimer;

    /// <summary>
    /// 指定された最大体力と基準位置で <see cref="BossStats"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="maxHp">最大体力値</param>
    /// <param name="baseX">浮動中心 X 座標</param>
    /// <param name="baseY">浮動中心 Y 座標</param>
    public BossStats(float maxHp = 90.0f, float baseX = 640.0f, float baseY = 180.0f)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Phase = 1;
        AttackTimer1 = 0.5f;
        AttackTimer2 = 1.0f;
        AngleOffset = 0.0f;
        HitFlashTimer = 0.0f;
        BaseX = baseX;
        BaseY = baseY;
        HoverTimer = 0.0f;
    }
}
