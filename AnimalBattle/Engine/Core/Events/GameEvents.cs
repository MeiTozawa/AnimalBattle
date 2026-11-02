namespace AnimalBattle.Events;

/// <summary>自機が地面からジャンプしたイベント</summary>
public readonly record struct PlayerJumpedEvent;

/// <summary>自機が空中二段ジャンプしたイベント</summary>
public readonly record struct PlayerDoubleJumpedEvent;

/// <summary>自機が一定速度以上で着地したイベント</summary>
public readonly record struct PlayerLandedEvent;

/// <summary>自機が射撃を行ったイベント</summary>
public readonly record struct PlayerShotFiredEvent(Vec2f SpawnPos, Vec2f Direction);

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
