using AnimalBattle.Logging;

namespace AnimalBattle.Core;

/// <summary>
///     ゲーム内で再生される効果音 SFX の種類を定義する列挙型。
/// </summary>
public enum SfxType
{
    PlayerShoot,
    PlayerJump,
    PlayerDoubleJump,
    PlayerLand,
    PlayerDash,
    PlayerHit,
    PlayerDeath,
    PlayerLowHealth,
    BossBulletShoot,
    BossHit,
    BossHitVulnerable,
    BossDiveImpact,
    BossWarning,
    BossDeath
}

/// <summary>
///     ゲーム内ドメインイベントを購読し、DxLib ネイティブサウンドパイプラインを通じて
///     BGM、効果音 (SFX)、および低体力警報を統括制御する音響サービス。
///     ゲームプレイシステムから完全に疎結合化されています。
/// </summary>
public static class SoundService
{
    private static readonly Dictionary<SfxType, int> SfxHandles = new();
    private static int _bgmHandle = -1;
    private static bool _isInitialized;

    /// <summary>現在アクティブな音声設定。</summary>
    public static AudioConfig CurrentConfig { get; set; } = new();

    /// <summary>
    ///     サウンドサービスが初期化済みかどうかを取得します。
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    ///     全 15 種類の音声アセットを読み込み、イベントバスへのリスナー登録を行って初期化します。
    ///     Debug ビルド時は Debug.Assert によりすべてのファイル存在および正常ロードを厳格に保証します。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        // BGM ロード
        var bgmPath = AssetPathResolver.Instance.ResolveAssetPath(ResourcePath.Sound.BgmBattleMp3);
        Debug.Assert(File.Exists(bgmPath), $"BGM 音声ファイルが見つかりません: {bgmPath}");
        if (!File.Exists(bgmPath)) Log.Error("SoundService", $"BGM ファイルが見つかりません: {bgmPath}");
        _bgmHandle = NativeBindings.Sound_Load(bgmPath);
        Debug.Assert(_bgmHandle >= 0, $"BGM 音声ファイルの読み込みに失敗しました (DxLib エラー): {bgmPath}");
        if (_bgmHandle < 0) Log.Error("SoundService", $"BGM ファイルの読み込みに失敗しました (DxLib エラー): {bgmPath}");

        // 各 SFX とファイルパスのマッピング
        ReadOnlySpan<(SfxType Type, string RelPath)> sfxDefinitions = [
            (SfxType.PlayerShoot, ResourcePath.Sound.PlayerShootMp3),
            (SfxType.PlayerJump, ResourcePath.Sound.PlayerJumpMp3),
            (SfxType.PlayerDoubleJump, ResourcePath.Sound.PlayerDoubleJumpMp3),
            (SfxType.PlayerLand, ResourcePath.Sound.PlayerLandMp3),
            (SfxType.PlayerDash, ResourcePath.Sound.PlayerDashMp3),
            (SfxType.PlayerHit, ResourcePath.Sound.PlayerHitMp3),
            (SfxType.PlayerDeath, ResourcePath.Sound.PlayerDeathMp3),
            (SfxType.PlayerLowHealth, ResourcePath.Sound.PlayerLowHealthMp3),
            (SfxType.BossBulletShoot, ResourcePath.Sound.BossBulletShootMp3),
            (SfxType.BossHit, ResourcePath.Sound.BulletHitBossMp3),
            (SfxType.BossHitVulnerable, ResourcePath.Sound.BulletHitVulnerableMp3),
            (SfxType.BossDiveImpact, ResourcePath.Sound.BossDiveImpactMp3),
            (SfxType.BossWarning, ResourcePath.Sound.BossWarningMp3),
            (SfxType.BossDeath, ResourcePath.Sound.BossDeathMp3)
        ];

        foreach (var (type, relPath) in sfxDefinitions)
        {
            var fullPath = AssetPathResolver.Instance.ResolveAssetPath(relPath);
            Debug.Assert(File.Exists(fullPath), $"SFX 音声ファイルが見つかりません: {fullPath} (SfxType: {type})");
            if (!File.Exists(fullPath)) Log.Error("SoundService", $"SFX ファイルが見つかりません: {fullPath} (SfxType: {type})");

            var handle = NativeBindings.Sound_Load(fullPath);
            Debug.Assert(handle >= 0, $"SFX 音声ファイルの読み込みに失敗しました (DxLib エラー): {fullPath} (SfxType: {type})");
            if (handle < 0) Log.Error("SoundService", $"SFX ファイルの読み込みに失敗しました (DxLib エラー): {fullPath} (SfxType: {type})");

            SfxHandles[type] = handle;
        }

        RegisterEventHandlers();
        _isInitialized = true;
        Log.Info("SoundService", $"[SoundService] サウンドエンジン初期化完了: BGM 1 件、SFX {SfxHandles.Count} 件をロードし、EventBus へ購読しました。");
    }

    /// <summary>
    ///     EventBus の各ドメインイベントを購読します。
    ///     多重登録を防止するため、既存の登録がある場合は事前に解除します。
    /// </summary>
    public static void RegisterEventHandlers()
    {
        UnregisterEventHandlers();

        EventBus.Subscribe<PlayerJumpedEvent>(OnPlayerJumped);
        EventBus.Subscribe<PlayerDoubleJumpedEvent>(OnPlayerDoubleJumped);
        EventBus.Subscribe<PlayerLandedEvent>(OnPlayerLanded);
        EventBus.Subscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed);
        EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        EventBus.Subscribe<CombatResetEvent>(OnCombatReset);
        EventBus.Subscribe<BossBulletFiredEvent>(OnBossBulletFired);
        EventBus.Subscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Subscribe<BossWarningEvent>(OnBossWarning);
        EventBus.Subscribe<BossDamagedEvent>(OnBossDamaged);
        EventBus.Subscribe<BossDiedEvent>(OnBossDied);
    }

    /// <summary>
    ///     EventBus の全ドメインイベントの購読を解除します。
    /// </summary>
    public static void UnregisterEventHandlers()
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
        EventBus.Unsubscribe<BossBulletFiredEvent>(OnBossBulletFired);
        EventBus.Unsubscribe<BossDiveImpactEvent>(OnBossDiveImpact);
        EventBus.Unsubscribe<BossWarningEvent>(OnBossWarning);
        EventBus.Unsubscribe<BossDamagedEvent>(OnBossDamaged);
        EventBus.Unsubscribe<BossDiedEvent>(OnBossDied);
    }

    // --- イベントハンドラー ---

    private static void OnPlayerJumped(PlayerJumpedEvent e) => PlaySfx(SfxType.PlayerJump);
    private static void OnPlayerDoubleJumped(PlayerDoubleJumpedEvent e) => PlaySfx(SfxType.PlayerDoubleJump);
    private static void OnPlayerLanded(PlayerLandedEvent e) => PlaySfx(SfxType.PlayerLand);
    private static void OnPlayerShotFired(PlayerShotFiredEvent e) => PlaySfx(SfxType.PlayerShoot);
    private static void OnPlayerDashed(PlayerDashedEvent e) => PlaySfx(SfxType.PlayerDash);
    private static void OnPlayerDamaged(PlayerDamagedEvent e) => PlaySfx(SfxType.PlayerHit);

    private static void OnPlayerDied(PlayerDiedEvent e) => PlaySfx(SfxType.PlayerDeath);

    private static void OnPlayerHealthChanged(PlayerHealthChangedEvent e)
    {
        if (e.PreviousHealth > 1.0f && Math.Abs(e.CurrentHealth - 1.0f) < 0.0001f)
        {
            PlaySfx(SfxType.PlayerLowHealth);
        }
    }

    private static void OnCombatReset(CombatResetEvent e)
    {
        ResetBgm();
    }

    private static void OnBossBulletFired(BossBulletFiredEvent e) => PlaySfx(SfxType.BossBulletShoot);
    private static void OnBossDiveImpact(BossDiveImpactEvent e) => PlaySfx(SfxType.BossDiveImpact);
    private static void OnBossWarning(BossWarningEvent e) => PlaySfx(SfxType.BossWarning);

    private static void OnBossDamaged(BossDamagedEvent e)
    {
        PlaySfx(e.IsVulnerable ? SfxType.BossHitVulnerable : SfxType.BossHit);
    }

    private static void OnBossDied(BossDiedEvent e) => PlaySfx(SfxType.BossDeath);

    /// <summary>
    ///     指定した効果音 (SFX) を再生します。
    /// </summary>
    /// <param name="type">効果音種別</param>
    /// <param name="volumeScale">相対音量スケール (0.0 ～ 1.0)</param>
    public static void PlaySfx(SfxType type, float volumeScale = 1.0f)
    {
        if (!SfxHandles.TryGetValue(type, out var handle) || handle < 0) return;

        var audioConfig = CurrentConfig;
        var finalVolume = Math.Clamp(audioConfig.MasterVolume * audioConfig.SfxVolume * volumeScale * 255.0f, 0.0f, 255.0f);
        NativeBindings.Sound_SetVolume(handle, (int)finalVolume);
        NativeBindings.Sound_Play(handle, 1, true); // DX_PLAYTYPE_BACK, 先頭から再生
    }

    /// <summary>
    ///     戦闘 BGM の再生を開始します。
    /// </summary>
    /// <param name="loop">ループ再生を行うかどうか</param>
    /// <param name="restart">先頭位置から再生し直すかどうか</param>
    public static void PlayBgm(bool loop = true, bool restart = true)
    {
        if (_bgmHandle < 0) return;

        var audioConfig = CurrentConfig;
        var finalVolume = Math.Clamp(audioConfig.MasterVolume * audioConfig.BgmVolume * 255.0f, 0.0f, 255.0f);
        NativeBindings.Sound_SetVolume(_bgmHandle, (int)finalVolume);
        NativeBindings.Sound_Play(_bgmHandle, loop ? 2 : 1, restart); // DX_PLAYTYPE_LOOP : DX_PLAYTYPE_BACK
    }

    /// <summary>
    ///     戦闘 BGM を停止し、再生位置を先頭に戻して再開（リセット）します。
    /// </summary>
    /// <param name="loop">ループ再生を行うかどうか</param>
    public static void ResetBgm(bool loop = true)
    {
        StopBgm();
        PlayBgm(loop, restart: true);
    }

    /// <summary>
    ///     戦闘 BGM を停止します。
    /// </summary>
    public static void StopBgm()
    {
        if (_bgmHandle >= 0)
        {
            NativeBindings.Sound_Stop(_bgmHandle);
        }
    }

    /// <summary>
    ///     ロード済みの全音声ハンドルを解放し、シャットダウンします。
    /// </summary>
    public static void Shutdown()
    {
        if (!_isInitialized) return;

        UnregisterEventHandlers();

        if (_bgmHandle >= 0)
        {
            NativeBindings.Sound_Stop(_bgmHandle);
            NativeBindings.Sound_Delete(_bgmHandle);
            _bgmHandle = -1;
        }

        foreach (var handle in SfxHandles.Values)
        {
            if (handle >= 0)
            {
                NativeBindings.Sound_Stop(handle);
                NativeBindings.Sound_Delete(handle);
            }
        }
        SfxHandles.Clear();

        _isInitialized = false;
        Log.Info("SoundService", "[SoundService] サウンドエンジンをシャットダウンし、全音声メモリを解放しました。");
    }
}
