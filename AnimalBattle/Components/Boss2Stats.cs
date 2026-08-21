namespace AnimalBattle.Components;

/// <summary>
/// Boss 2 (Ground Crusher) の実行時ステータス、物理状態および巡回パラメータを保持するコンポーネント。
/// </summary>
public struct Boss2Stats
{
    /// <summary>現在の戦闘段階 (Phase 1, 2, 3)</summary>
    public int Phase;

    /// <summary>出現基準 X 座標</summary>
    public float SpawnX;

    /// <summary>出現基準 Y 座標</summary>
    public float SpawnY;

    /// <summary>現在の水平向き（true: 右向き, false: 左向き）</summary>
    public bool FacingRight;

    /// <summary>巡回・追尾移動タイマー（秒）</summary>
    public float PatrolTimer;

    /// <summary>跳躍クールダウンタイマー（秒）</summary>
    public float JumpCooldown;

    /// <summary>被弾ホワイトフラッシュ演出タイマー（秒）</summary>
    public float HitFlashTimer;

    /// <summary>連続攻撃コンボ実行インデックス</summary>
    public int ComboIndex;

    /// <summary>
    /// <see cref="Boss2Stats"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="spawnX">出現基準 X 座標</param>
    /// <param name="spawnY">出現基準 Y 座標</param>
    public Boss2Stats(float spawnX = 900.0f, float spawnY = 560.0f)
    {
        Phase = 1;
        SpawnX = spawnX;
        SpawnY = spawnY;
        FacingRight = false;
        PatrolTimer = 0.0f;
        JumpCooldown = 0.0f;
        HitFlashTimer = 0.0f;
        ComboIndex = 0;
    }
}
