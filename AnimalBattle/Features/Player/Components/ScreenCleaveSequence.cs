using System.Drawing;

namespace AnimalBattle.Player.Components;

/// <summary>
///     Bunny の特殊能力（全画面連続斬撃）の視覚エフェクトシーケンス実行状態を保持するコンポーネント。
/// </summary>
public struct ScreenCleaveSequence
{
    /// <summary>シーケンス開始からの経過時間（秒）</summary>
    public float ElapsedTimer;

    /// <summary>全画面斬撃エフェクトの総持続時間（秒）</summary>
    public float TotalDuration;

    /// <summary>次回斬撃エフェクト生成予定時刻（秒）</summary>
    public float NextSpawnTimer;

    /// <summary>すでに生成された斬撃エフェクト数</summary>
    public int SpawnedCount;

    /// <summary>生成予定の斬撃エフェクト総数</summary>
    public int TargetCount;

    /// <summary>単一斬撃エフェクトの生存時間（秒）</summary>
    public float SlashLifetime;

    /// <summary>斬撃エフェクト最小スケール</summary>
    public float ScaleMin;

    /// <summary>斬撃エフェクト最大スケール</summary>
    public float ScaleMax;

    /// <summary>斬撃エフェクトの推進速度（px/s）</summary>
    public float SlashSpeed;

    /// <summary>斬撃エフェクトの表示色</summary>
    public Color SlashColor;

    /// <summary>斬撃スプライトのグラフィックハンドル</summary>
    public int GraphHandle;

    /// <summary>斬撃スプライトのセルサイズ</summary>
    public int CellSize;

    /// <summary>斬撃スプライトの列インデックス</summary>
    public int ColIndex;

    /// <summary>全画面斬撃による期待総ダメージ量</summary>
    public float TotalDamage;

    /// <summary>継続ダメージの発生ティック間隔（秒）</summary>
    public float TickInterval;

    /// <summary>次回継続ダメージ発生予定時刻（秒）</summary>
    public float NextDamageTimer;

    /// <summary>これまでに敵対ユニットへ与えた累積ダメージ量</summary>
    public float DealtDamage;
}
