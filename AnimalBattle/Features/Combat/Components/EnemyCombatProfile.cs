namespace AnimalBattle.Combat.Components;

/// <summary>
///     敵対エンティティのHPゼロ時における死亡処理挙動を定義します。
/// </summary>
public enum EnemyDeathBehavior : byte
{
    /// <summary>小怪・雑魚敵：死亡エフェクト再生後に DeadTag を付与して破棄します。</summary>
    DestroyOnDeath = 0,

    /// <summary>ステージBoss：勝利演出、スローモーション、カメラ振動、GameOverTag を付与します。</summary>
    BossVictory = 1,

    /// <summary>訓練用木桩：撃破されず、HPゼロ時に即時全回復して計測を継続します。</summary>
    ImmortalReset = 2
}

/// <summary>
///     敵対エンティティの戦闘リアクション・被撃・死亡ポリシーを規定するデータコンポーネント。
/// </summary>
public struct EnemyCombatProfile
{
    /// <summary>死亡時の処理挙動。</summary>
    public EnemyDeathBehavior DeathBehavior;

    /// <summary>ノックバックや打ち上げ浮力を受けるか。</summary>
    public bool CanBeKnockedBack;

    /// <summary>被撃時に反撃弾幕を展開するか。</summary>
    public bool HasCounterAttack;

    /// <summary>Boss用の戦闘プロファイルを生成します。</summary>
    public static EnemyCombatProfile ForBoss(bool knockback) => new()
    {
        DeathBehavior = EnemyDeathBehavior.BossVictory,
        CanBeKnockedBack = knockback,
        HasCounterAttack = true
    };

    /// <summary>一般敵・雑魚敵用の戦闘プロファイルを生成します。</summary>
    public static EnemyCombatProfile ForMinion() => new()
    {
        DeathBehavior = EnemyDeathBehavior.DestroyOnDeath,
        CanBeKnockedBack = true,
        HasCounterAttack = false
    };

    /// <summary>手触り検証用ダミー木桩の戦闘プロファイルを生成します。</summary>
    public static EnemyCombatProfile ForDummy() => new()
    {
        DeathBehavior = EnemyDeathBehavior.ImmortalReset,
        CanBeKnockedBack = true,
        HasCounterAttack = false
    };
}
