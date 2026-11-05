namespace AnimalBattle.Features.Items.Configs;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Config/items.toml から読み込まれるアイテムゲームプレイ全般の設定クラス。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class ItemConfig
{
    /// <summary>回復アイテム（ヘルスパック）の設定データ。</summary>
    public HealthPackConfig HealthPack { get; set; } = null!;
}

/// <summary>
///     回復アイテム（ヘルスパック）のドロップ確率・寿命・物理挙動の設定データ。
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class HealthPackConfig
{
    /// <summary>フィールド上の最大同時存在数。</summary>
    public int MaxActiveCount { get; set; }

    /// <summary>ドロップ判定の内部クールダウン時間（秒）。</summary>
    public float IcdDuration { get; set; }

    /// <summary>ドロップ後の自然消滅時間（秒）。</summary>
    public float Lifetime { get; set; }

    /// <summary>消滅前の警告点滅を開始する残り時間（秒）。</summary>
    public float BlinkThreshold { get; set; }

    /// <summary>プレイヤー回収の接触判定半径（px）。</summary>
    public float PickupRadius { get; set; }

    /// <summary>取得時に回復する HP 量。</summary>
    public float HealAmount { get; set; }

    /// <summary>基本ドロップ率。</summary>
    public float BaseDropRate { get; set; }

    /// <summary>プレイヤーの残り HP に応じたドロップ率補正の重み。</summary>
    public float PlayerWeight { get; set; }

    /// <summary>敵の残り HP に応じたドロップ率補正の重み。</summary>
    public float BossWeight { get; set; }

    /// <summary>与ダメージ量に応じたドロップ率の乗算倍率。</summary>
    public float DamageScale { get; set; }

    /// <summary>ドロップ率の上限値。</summary>
    public float MaxDropRate { get; set; }

    /// <summary>ドロップ時の水平初速度の最小値（px/s）。</summary>
    public float PopVelocityXMin { get; set; }

    /// <summary>ドロップ時の水平初速度の最大値（px/s）。</summary>
    public float PopVelocityXMax { get; set; }

    /// <summary>ドロップ時の垂直初速度（px/s、上向きは負値）。</summary>
    public float PopVelocityY { get; set; }

    /// <summary>落下重力加速度（px/s²）。</summary>
    public float Gravity { get; set; }

    /// <summary>接地後の浮遊アニメーション角速度。</summary>
    public float FloatSpeed { get; set; }

    /// <summary>接地後の浮遊アニメーション振幅（px）。</summary>
    public float FloatAmplitude { get; set; }

    /// <summary>地形および足場との衝突判定サイズ（px）。</summary>
    public Vec2f CollisionSize { get; set; }
}
