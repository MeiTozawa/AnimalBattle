using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Logging;

namespace AnimalBattle.Presentation.Audio;

/// <summary>
///     ゲーム内ドメインイベントを購読し、対応する効果音 (SFX) や BGM 操作を
///     <see cref="SoundService"/> へディスパッチする表現層音響フィードバックマネージャー。
///     コンストラクタでイベント購読を開始し、<see cref="Dispose"/> で確実に解除します。
/// </summary>
public sealed class AudioFeedback : IDisposable
{
    private bool _isDisposed;

    /// <summary>
    ///     <see cref="AudioFeedback"/> のインスタンスを初期化し、全ドメインイベントの購読を開始します。
    /// </summary>
    public AudioFeedback()
    {
        RegisterEventHandlers();
        Log.Info("AudioFeedback", "[AudioFeedback] イベント購読を開始しました。");
    }

    /// <summary>
    ///     全ドメインイベントの購読を解除し、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        UnregisterEventHandlers();
        _isDisposed = true;
        Log.Info("AudioFeedback", "[AudioFeedback] イベント購読を解除しました。");
    }

    private void RegisterEventHandlers()
    {
        EventBus.Subscribe<PlayerJumpedEvent>(OnPlayerJumped);
        EventBus.Subscribe<PlayerDoubleJumpedEvent>(OnPlayerDoubleJumped);
        EventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
        EventBus.Subscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed);
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        EventBus.Subscribe<CombatResetEvent>(OnCombatReset);
        EventBus.Subscribe<EnemyBulletFiredEvent>(OnEnemyBulletFired);
        EventBus.Subscribe<BossBulletFiredEvent>(OnBossBulletFired);
        EventBus.Subscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Subscribe<BossDamagedEvent>(OnBossDamaged);
        EventBus.Subscribe<BossDiedEvent>(OnBossDied);
        EventBus.Subscribe<PlayerMeleeSwungEvent>(OnPlayerMeleeSwung);
        EventBus.Subscribe<ItemSpawnedEvent>(OnItemSpawned);
        EventBus.Subscribe<ItemCollectedEvent>(OnItemCollected);
        EventBus.Subscribe<BulletCancelledEvent>(OnBulletCancelled);
        EventBus.Subscribe<ScreenCleaveExecutedEvent>(OnScreenCleaveExecuted);
        EventBus.Subscribe<CombatHitLandedEvent>(OnCombatHitLanded);
        EventBus.Subscribe<PlaySfxRequestEvent>(OnPlaySfxRequested);
    }

    private void UnregisterEventHandlers()
    {
        EventBus.Unsubscribe<PlayerJumpedEvent>(OnPlayerJumped);
        EventBus.Unsubscribe<PlayerDoubleJumpedEvent>(OnPlayerDoubleJumped);
        EventBus.Unsubscribe<PlayerLandedEvent>(OnPlayerLanded);
        EventBus.Unsubscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        EventBus.Unsubscribe<PlayerDashedEvent>(OnPlayerDashed);
        EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        EventBus.Unsubscribe<CombatResetEvent>(OnCombatReset);
        EventBus.Unsubscribe<EnemyBulletFiredEvent>(OnEnemyBulletFired);
        EventBus.Unsubscribe<BossBulletFiredEvent>(OnBossBulletFired);
        EventBus.Unsubscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Unsubscribe<BossDamagedEvent>(OnBossDamaged);
        EventBus.Unsubscribe<BossDiedEvent>(OnBossDied);
        EventBus.Unsubscribe<PlayerMeleeSwungEvent>(OnPlayerMeleeSwung);
        EventBus.Unsubscribe<ItemSpawnedEvent>(OnItemSpawned);
        EventBus.Unsubscribe<ItemCollectedEvent>(OnItemCollected);
        EventBus.Unsubscribe<BulletCancelledEvent>(OnBulletCancelled);
        EventBus.Unsubscribe<ScreenCleaveExecutedEvent>(OnScreenCleaveExecuted);
        EventBus.Unsubscribe<CombatHitLandedEvent>(OnCombatHitLanded);
        EventBus.Unsubscribe<PlaySfxRequestEvent>(OnPlaySfxRequested);
    }

    // --- イベントハンドラー ---

    private static void OnPlayerJumped(PlayerJumpedEvent e) => SoundService.PlaySfx(SfxType.PlayerJump);
    private static void OnPlayerDoubleJumped(PlayerDoubleJumpedEvent e) => SoundService.PlaySfx(SfxType.PlayerDoubleJump);
    private static void OnPlayerLanded(PlayerLandedEvent e) => SoundService.PlaySfx(SfxType.PlayerLand);
    private static void OnPlayerShotFired(PlayerShotFiredEvent e) => SoundService.PlaySfx(SfxType.PlayerShoot);
    private static void OnPlayerDashed(PlayerDashedEvent e) => SoundService.PlaySfx(SfxType.PlayerDash);
    private static void OnPlayerDamaged(PlayerDamagedEvent e) => SoundService.PlaySfx(SfxType.PlayerHit);
    private static void OnPlayerDied(PlayerDiedEvent e) => SoundService.PlaySfx(SfxType.PlayerDeath);

    private static void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (e.PreviousHealth > 1.0f && Math.Abs(e.CurrentHealth - 1.0f) < 0.0001f)
        {
            SoundService.PlaySfx(SfxType.PlayerLowHealth);
        }
    }

    private static void OnCombatReset(CombatResetEvent e) => SoundService.ResetBgm();
    private static void OnEnemyBulletFired(EnemyBulletFiredEvent e) => SoundService.PlaySfx(SfxType.EnemyBulletShoot);
    private static void OnBossBulletFired(BossBulletFiredEvent e) => SoundService.PlaySfx(SfxType.EnemyBulletShoot);
    private static void OnBossDiveImpact(BossDiveImpactEvent e) => SoundService.PlaySfx(SfxType.BossDiveImpact);
    private static void OnBossDamaged(BossDamagedEvent e) => SoundService.PlaySfx(e.IsVulnerable ? SfxType.BossHitVulnerable : SfxType.BossHit);
    private static void OnBossDied(BossDiedEvent e) => SoundService.PlaySfx(SfxType.BossDeath);
    private static void OnPlayerMeleeSwung(PlayerMeleeSwungEvent e) => SoundService.PlaySfx(SfxType.PlayerDash);
    private static void OnItemSpawned(ItemSpawnedEvent e) => SoundService.PlaySfx(SfxType.ItemDrop);
    private static void OnItemCollected(ItemCollectedEvent e) => SoundService.PlaySfx(SfxType.ItemHeal);
    private static void OnBulletCancelled(BulletCancelledEvent e) => SoundService.PlaySfx(SfxType.BossHit);
    private static void OnCombatHitLanded(CombatHitLandedEvent e) => SoundService.PlaySfx(SfxType.BossHit);
    private static void OnPlaySfxRequested(PlaySfxRequestEvent e) => SoundService.PlaySfx(e.Type, e.VolumeScale);

    private static void OnScreenCleaveExecuted(ScreenCleaveExecutedEvent e)
    {
        SoundService.PlaySfx(SfxType.BossDiveImpact);
        if (e.ClearedBullets > 0)
        {
            SoundService.PlaySfx(SfxType.BossHit);
        }
    }
}
