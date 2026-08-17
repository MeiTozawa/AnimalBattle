using System;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Config/player.toml から読み込まれるプレイヤー動作・物理・判定パラメータの設定クラス。
/// 形状種別、寸法、および局所オフセット (HurtboxOffsetX/Y, BulletHitboxOffsetX/Y) を完全管理します。
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

    /// <summary>プレイヤー被弾判定 (Hurtbox) の幾何形状種別（"Circle" または "Box"）</summary>
    public string HurtboxShape { get; set; } = "Circle";

    /// <summary>プレイヤー被弾判定円半径（HurtboxShape == "Circle" 時に使用）</summary>
    public float HurtboxRadius { get; set; } = 7.0f;

    /// <summary>プレイヤー被弾判定矩形幅（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxWidth { get; set; } = 0.0f;

    /// <summary>プレイヤー被弾判定矩形高さ（HurtboxShape == "Box" 時に使用）</summary>
    public float HurtboxHeight { get; set; } = 0.0f;

    /// <summary>プレイヤー被弾判定の X 軸局所オフセット（ピクセル）</summary>
    public float HurtboxOffsetX { get; set; } = 0.0f;

    /// <summary>プレイヤー被弾判定の Y 軸局所オフセット（ピクセル、下向き正値で胸腹部へ微調整）</summary>
    public float HurtboxOffsetY { get; set; } = 4.0f;

    /// <summary>敵弾に対するプレイヤー当たり判定半径（後方互換プロパティ）</summary>
    public float HitboxRadius => HurtboxRadius;

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

    /// <summary>プレイヤー銃弾攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string BulletHitboxShape { get; set; } = "Box";

    /// <summary>銃弾当たり判定幅（BulletHitboxShape == "Box" 時に使用）</summary>
    public float BulletWidth { get; set; } = 12.0f;

    /// <summary>銃弾当たり判定高さ（BulletHitboxShape == "Box" 時に使用）</summary>
    public float BulletHeight { get; set; } = 8.0f;

    /// <summary>銃弾当たり判定円半径（BulletHitboxShape == "Circle" 時に使用）</summary>
    public float BulletRadius { get; set; } = 0.0f;

    /// <summary>銃弾判定の X 軸局所オフセット</summary>
    public float BulletHitboxOffsetX { get; set; } = 0.0f;

    /// <summary>銃弾判定の Y 軸局所オフセット</summary>
    public float BulletHitboxOffsetY { get; set; } = 0.0f;

    /// <summary>銃弾 1 発あたりの与ダメージ量</summary>
    public float BulletDamage { get; set; } = 1.0f;

    /// <summary>
    /// 設定に基づいたプレイヤー被弾判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShape.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight, HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    /// 設定に基づいたプレイヤー銃弾攻撃判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された銃弾攻撃衝突形状バリアント</returns>
    public CollisionShape GetBulletHitboxShape()
    {
        return CollisionShape.CreateWithFallback(BulletHitboxShape, BulletRadius, BulletWidth, BulletHeight, BulletHitboxOffsetX, BulletHitboxOffsetY);
    }

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
