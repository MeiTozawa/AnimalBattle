using System;
using System.Collections.Generic;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// Boss 2 (Ground Crusher) 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class Boss2PhaseConfig
{
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; } = 1.0f;

    /// <summary>攻撃前の待機巡航時間（秒）</summary>
    public float IdleDuration { get; set; } = 0.8f;

    /// <summary>このフェーズでの地面移動速度</summary>
    public float MoveSpeed { get; set; } = 160.0f;

    /// <summary>このフェーズでの跳躍初速</summary>
    public float JumpForce { get; set; } = 580.0f;

    /// <summary>蓄力予兆時の機体振動振幅（ピクセル）</summary>
    public float TelegraphShakeIntensity { get; set; } = 6.0f;

    /// <summary>フェーズ突入時の無敵・吼叫演出時間（秒）</summary>
    public float TransitionDuration { get; set; } = 1.0f;

    /// <summary>このフェーズで実行される技能名リスト</summary>
    public List<string> Skills { get; set; } = [];
}

/// <summary>
/// Config/boss2.toml から読み込まれる Boss 2 (Ground Crusher) の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class Boss2Config
{
    /// <summary>Boss 表示名</summary>
    public string BossName { get; set; } = "GROUND CRUSHER";

    /// <summary>Boss の最大 HP</summary>
    public float MaxHp { get; set; } = 80.0f;

    /// <summary>Boss の基準出現 X 座標</summary>
    public float SpawnX { get; set; } = 900.0f;

    /// <summary>Boss の基準出現 Y 座標</summary>
    public float SpawnY { get; set; } = 480.0f;

    /// <summary>Boss 描画スケール X</summary>
    public float ScaleX { get; set; } = 0.65f;

    /// <summary>Boss 描画スケール Y</summary>
    public float ScaleY { get; set; } = 0.65f;

    /// <summary>Boss 被弾判定 (Hurtbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HurtboxShape { get; set; } = "Box";

    /// <summary>Boss 被弾当たり判定幅</summary>
    public float HurtboxWidth { get; set; } = 90.0f;

    /// <summary>Boss 被弾当たり判定高さ</summary>
    public float HurtboxHeight { get; set; } = 110.0f;

    /// <summary>Boss 被弾当たり判定円半径</summary>
    public float HurtboxRadius { get; set; } = 50.0f;

    /// <summary>Boss 被弾判定の X 軸局所オフセット</summary>
    public float HurtboxOffsetX { get; set; } = 0.0f;

    /// <summary>Boss 被弾判定の Y 軸局所オフセット</summary>
    public float HurtboxOffsetY { get; set; } = 0.0f;

    /// <summary>Boss 身体接触攻撃判定 (Hitbox) の幾何形状種別（"Box" または "Circle"）</summary>
    public string HitboxShape { get; set; } = "Box";

    /// <summary>Boss 接触攻撃判定幅</summary>
    public float BodyHitboxWidth { get; set; } = 80.0f;

    /// <summary>Boss 接触攻撃判定高さ</summary>
    public float BodyHitboxHeight { get; set; } = 100.0f;

    /// <summary>Boss 接触攻撃判定円半径</summary>
    public float BodyHitboxRadius { get; set; } = 45.0f;

    /// <summary>Boss 接触判定の X 軸局所オフセット</summary>
    public float BodyHitboxOffsetX { get; set; } = 0.0f;

    /// <summary>Boss 接触判定の Y 軸局所オフセット</summary>
    public float BodyHitboxOffsetY { get; set; } = 0.0f;

    /// <summary>Boss 身体接触命中時の与ダメージ量</summary>
    public float BodyContactDamage { get; set; } = 1.0f;

    /// <summary>地形衝突判定幅</summary>
    public float CollisionWidth { get; set; } = 70.0f;

    /// <summary>地形衝突判定高さ</summary>
    public float CollisionHeight { get; set; } = 90.0f;

    /// <summary>基本地面移動速度</summary>
    public float MoveSpeed { get; set; } = 160.0f;

    /// <summary>基本跳躍初速</summary>
    public float JumpForce { get; set; } = 580.0f;

    /// <summary>重力加速度</summary>
    public float Gravity { get; set; } = 980.0f;

    /// <summary>巡回パトロール半径</summary>
    public float PatrolRadius { get; set; } = 250.0f;

    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; } = 0.08f;

    /// <summary>Phase 1 状態機設定</summary>
    public Boss2PhaseConfig Phase1 { get; set; } = new()
    {
        HpThreshold = 1.0f,
        IdleDuration = 0.8f,
        MoveSpeed = 160.0f,
        JumpForce = 580.0f,
        TelegraphShakeIntensity = 6.0f,
        TransitionDuration = 1.0f,
        Skills = ["HeavySlash", "GroundStomp", "HeavySlash", "LeapSmash"]
    };

    /// <summary>Phase 2 状態機設定</summary>
    public Boss2PhaseConfig Phase2 { get; set; } = new()
    {
        HpThreshold = 0.66f,
        IdleDuration = 0.5f,
        MoveSpeed = 220.0f,
        JumpForce = 640.0f,
        TelegraphShakeIntensity = 10.0f,
        TransitionDuration = 1.0f,
        Skills = ["DoubleCleave", "GroundStomp", "LeapSmash", "DoubleCleave"]
    };

    /// <summary>Phase 3 状態機設定</summary>
    public Boss2PhaseConfig Phase3 { get; set; } = new()
    {
        HpThreshold = 0.33f,
        IdleDuration = 0.3f,
        MoveSpeed = 280.0f,
        JumpForce = 700.0f,
        TelegraphShakeIntensity = 14.0f,
        TransitionDuration = 1.2f,
        Skills = ["FrenzyCharge", "EarthquakeRoar", "DoubleCleave", "LeapSmash"]
    };

    /// <summary>
    /// 設定に基づいた Boss 2 被弾判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された被弾衝突形状バリアント</returns>
    public CollisionShape GetHurtboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HurtboxShape, HurtboxRadius, HurtboxWidth, HurtboxHeight, HurtboxOffsetX, HurtboxOffsetY);
    }

    /// <summary>
    /// 設定に基づいた Boss 2 接触攻撃判定 (<see cref="CollisionShape"/>) を取得します。
    /// </summary>
    /// <returns>構築された接触攻撃衝突形状バリアント</returns>
    public CollisionShape GetHitboxShape()
    {
        return CollisionShapeFactory.CreateWithFallback(HitboxShape, BodyHitboxRadius, BodyHitboxWidth, BodyHitboxHeight, BodyHitboxOffsetX, BodyHitboxOffsetY);
    }

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{Boss2Config}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<Boss2Config>> LoadFromFile(string path = ResourcePath.Config.Boss2)
    {
        return ConfigBase<Boss2Config>.LoadFromFile(path);
    }
}
