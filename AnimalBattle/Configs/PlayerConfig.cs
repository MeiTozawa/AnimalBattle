using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Config/player.toml から読み込まれるプレイヤー動作・物理・判定パラメータの設定クラス。
/// </summary>
public sealed class PlayerConfig
{
    /// <summary>プレイヤーの最大 HP（初期値: 3）</summary>
    public int MaxHp { get; set; } = 3;

    /// <summary>被弾後の無敵時間（秒、初期値: 0.5s）</summary>
    public float InvincibleDuration { get; set; } = 0.5f;

    /// <summary>プレイヤーの左右移動速度（ピクセル/秒基準）</summary>
    public float MoveSpeed { get; set; } = 4.8f;

    /// <summary>地上初段ジャンプの初速度（上向き負値）</summary>
    public float FirstJumpVelocity { get; set; } = -11.2f;

    /// <summary>空中二段ジャンプの初速度（上向き負値）</summary>
    public float SecondJumpVelocity { get; set; } = -9.2f;

    /// <summary>下向き重力加速度</summary>
    public float Gravity { get; set; } = 0.45f;

    /// <summary>最大落下終端速度</summary>
    public float MaxFallSpeed { get; set; } = 10.0f;

    /// <summary>最大跳躍可能回数（空中二段ジャンプ）</summary>
    public int MaxJumps { get; set; } = 2;

    /// <summary>敵弾に対するプレイヤー当たり判定半径（ピクセル）</summary>
    public float HitboxRadius { get; set; } = 7.0f;

    /// <summary>地形足場との衝突幅（ピクセル）</summary>
    public float HitboxWidth { get; set; } = 32.0f;

    /// <summary>地形足場との衝突高さ（ピクセル）</summary>
    public float HitboxHeight { get; set; } = 48.0f;

    /// <summary>プレイヤー描画スケール X</summary>
    public float ScaleX { get; set; } = 2.0f;

    /// <summary>プレイヤー描画スケール Y</summary>
    public float ScaleY { get; set; } = 2.0f;

    /// <summary>初期出現およびリスポーン X 座標</summary>
    public float SpawnX { get; set; } = 320.0f;

    /// <summary>初期出現およびリスポーン Y 座標</summary>
    public float SpawnY { get; set; } = 500.0f;

    /// <summary>手銃射撃のクールダウン間隔（秒）</summary>
    public float ShootCooldown { get; set; } = 0.16f;

    /// <summary>手銃弾の飛行速度</summary>
    public float BulletSpeed { get; set; } = 16.0f;

    /// <summary>銃弾生成時のプレイヤー中心からの X オフセット</summary>
    public float BulletSpawnOffset { get; set; } = 24.0f;

    /// <summary>銃弾生成時のプレイヤー中心からの Y オフセット</summary>
    public float BulletSpawnOffsetY { get; set; } = -4.0f;

    /// <summary>銃弾当たり判定幅</summary>
    public float BulletWidth { get; set; } = 12.0f;

    /// <summary>銃弾当たり判定高さ</summary>
    public float BulletHeight { get; set; } = 8.0f;

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{PlayerConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<PlayerConfig>> LoadFromFile(string path = "player.toml")
    {
        return ConfigBase<PlayerConfig>.LoadFromFile(path);
    }
}
