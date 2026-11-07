namespace AnimalBattle.Events;

/// <summary>自機が地面からジャンプしたイベント</summary>
public readonly record struct PlayerJumpedEvent;

/// <summary>自機が空中二段ジャンプしたイベント</summary>
public readonly record struct PlayerDoubleJumpedEvent;

/// <summary>自機が一定速度以上で着地したイベント</summary>
public readonly record struct PlayerLandedEvent;

/// <summary>自機が射撃を行ったイベント</summary>
public readonly record struct PlayerShotFiredEvent(Vec2f SpawnPos, Vec2f Direction);

/// <summary>自機が近接攻撃（突進・斬撃）を繰り出したイベント</summary>
public readonly record struct PlayerMeleeSwungEvent(Vec2f Position, Vec2f Direction);

/// <summary>自機がダッシュを開始したイベント：Task 4 用</summary>
public readonly record struct PlayerDashedEvent;

/// <summary>自機が被弾ダメージを受けたイベント</summary>
public readonly record struct PlayerDamagedEvent;

/// <summary>自機が死亡したイベント</summary>
public readonly record struct PlayerDiedEvent;

/// <summary>自機の体力が変化したイベント：残血警報等のリアクティブ判定用</summary>
public readonly record struct PlayerHealthChangedEvent(float PreviousHealth, float CurrentHealth);

/// <summary>エネミーが弾幕波次を発射したイベント</summary>
public readonly record struct EnemyBulletFiredEvent;

/// <summary>Boss が弾幕波次を発射したイベント（互換用エイリアス）</summary>
public readonly record struct BossBulletFiredEvent;

/// <summary>Boss が急降下衝撃波を発生させたイベント</summary>
public readonly record struct BossDiveImpactEvent;

/// <summary>Boss が被弾ダメージを受けたイベント</summary>
public readonly record struct BossDamagedEvent(bool IsVulnerable);

/// <summary>Boss が撃破死亡したイベント</summary>
public readonly record struct BossDiedEvent;

/// <summary>戦闘がリセットまたは開始されたイベント</summary>
public readonly record struct CombatResetEvent;

/// <summary>アイテムがドロップ・生成されたイベント</summary>
public readonly record struct ItemSpawnedEvent(Vec2f Position);

/// <summary>アイテムがプレイヤーに回収・取得されたイベント</summary>
public readonly record struct ItemCollectedEvent(Entity Picker, float Amount = 0.0f);

/// <summary>近接攻撃等により敵弾が相殺・消去されたイベント</summary>
public readonly record struct BulletCancelledEvent(Vec2f Position);

/// <summary>全画面斬撃等の特殊能力による連続判定が発動・実行されたイベント</summary>
public readonly record struct ScreenCleaveExecutedEvent(int ClearedBullets);

/// <summary>攻撃・持続判定が対象に命中したイベント</summary>
public readonly record struct CombatHitLandedEvent(bool IsHeavy = false);

/// <summary>汎用効果音再生リクエストイベント（スクリプト・演出・UI用の疎結合リクエスト）</summary>
public readonly record struct PlaySfxRequestEvent(SfxType Type, float VolumeScale = 1.0f);
