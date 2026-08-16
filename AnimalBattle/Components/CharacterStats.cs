namespace AnimalBattle.Components;

/// <summary>
/// キャラクター属性（体重・ダメージ率・Hitstun）を表すコンポーネント。
/// </summary>
public struct CharacterStats
{
    /// <summary>キャラクターの体重（ノックバック減衰率）</summary>
    public float Weight;

    /// <summary>累積ダメージ率（パーセンテージ）</summary>
    public float DamagePercent;

    /// <summary>ヒットストップ・ヒットスタンタイマー（秒）</summary>
    public float HitstunTimer;

    /// <summary>攻撃クールダウンタイマー（秒）</summary>
    public float AttackCooldown;

    /// <summary>
    /// 指定された体重で <see cref="CharacterStats"/> の新しいインスタンスを初期化します。
    /// </summary>
    public CharacterStats(float weight = 100.0f)
    {
        Weight = weight;
        DamagePercent = 0.0f;
        HitstunTimer = 0.0f;
        AttackCooldown = 0.0f;
    }
}
