namespace AnimalBattle.Core;

/// <summary>
/// Config/physics.toml から読み込まれるグローバル物理エンジンパラメータの設定クラス。
/// </summary>
public sealed class PhysicsConfig
{
    /// <summary>1ステップの最大デルタタイム (秒)</summary>
    public float MaxDeltaTime { get; set; } = 0.1f;

    /// <summary>ノックバック速度の減衰率</summary>
    public float KnockbackDecay { get; set; } = 400.0f;

    /// <summary>ノックバック重量補正オフセット</summary>
    public float KnockbackWeightOffset { get; set; } = 100.0f;

    /// <summary>ダメージ率スケール1</summary>
    public float KnockbackDamageScale1 { get; set; } = 10.0f;

    /// <summary>ダメージ率スケール2</summary>
    public float KnockbackDamageScale2 { get; set; } = 20.0f;

    /// <summary>ノックバックスケール倍率</summary>
    public float KnockbackScaleMultiplier { get; set; } = 1.4f;

    /// <summary>ノックバック基本倍率</summary>
    public float KnockbackBaseMultiplier { get; set; } = 18.0f;

    /// <summary>基本ヒットスタン時間 (秒)</summary>
    public float HitstunBaseSeconds { get; set; } = 0.25f;

    /// <summary>吹っ飛び力に応じた追加ヒットスタン係数</summary>
    public float HitstunKnockbackScale { get; set; } = 0.0005f;

    /// <summary>最大ヒットスタン時間制限 (秒)</summary>
    public float HitstunMaxSeconds { get; set; } = 1.2f;

    /// <summary>すり抜け床通過時のY押し出しオフセット</summary>
    public float DropThroughPushOffset { get; set; } = 3.0f;

    /// <summary>リスポーン位置のY座標</summary>
    public float RespawnY { get; set; } = 150.0f;

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{PhysicsConfig}"/> インスタンスを生成します。
    /// </summary>
    public static Result<ConfigBase<PhysicsConfig>> LoadFromFile(string path = "physics.toml")
    {
        return ConfigBase<PhysicsConfig>.LoadFromFile(path);
    }
}
