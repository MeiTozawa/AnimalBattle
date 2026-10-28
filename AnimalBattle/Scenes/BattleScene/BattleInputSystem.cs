using AnimalBattle.Ai;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される戦闘入力処理システム用コンテキスト。
/// </summary>
internal sealed class BattleInputSystemContext : IDisposable
{
    public required Query PlayerBulletsQuery { get; init; }
    public required Query VfxQuery { get; init; }
    public required Query PlayerEntitiesQuery { get; init; }
    public required Query<Health, BossCombatState, BossBrainComponent> BossQuery { get; init; }

    public void Dispose()
    {
        PlayerBulletsQuery.Dispose();
        VfxQuery.Dispose();
        PlayerEntitiesQuery.Dispose();
        BossQuery.Dispose();
    }
}

/// <summary>
///     戦闘シーンにおける入力受付、自機操作、および戦闘リセット・シーン復帰を司る ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、PreUpdate フェーズでプレイヤーの入力を処理します。
/// </summary>
public static class BattleInputSystem
{
    private static bool _prevF1;
    private static bool _prevF2;
    private static bool _prevF3;
    private static bool _prevF4;
    private static bool _prevF5;

    private static Phase _targetDebugPhase;

    /// <summary>
    ///     戦闘入力処理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleInputSystem(this World world)
    {
        var playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        var vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        var playerEntitiesQuery = world.QueryBuilder()
            .With<PlayerTag>()
            .Build();
        var bossQuery = world.QueryBuilder<Health, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Build();

        world.Set(new BattleInputSystemContext
        {
            PlayerBulletsQuery = playerBulletsQuery,
            VfxQuery = vfxQuery,
            PlayerEntitiesQuery = playerEntitiesQuery,
            BossQuery = bossQuery
        });

        world.System("BattleInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteBattleInput);

        return world;
    }

    /// <summary>
    ///     戦闘シーンの入力受付パイプライン。
    /// </summary>
    public static void ExecuteBattleInput(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var ctx = w.Get<BattleInputSystemContext>();

        // ESC キーでタイトルまたはキャラクター選択画面へ復帰
        if (InputService.IsPressed(InputAction.Cancel))
        {
            GameManager.Current.SwitchToStart();
            return;
        }

        // R キーで戦闘リセット
        if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCombat(w, ctx);
        }

        // デバッグ入力：F1 トグル、F2〜F4 フェーズ切替、F5 空シーン切替の処理
        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;
        if (dbgCfg.DebugMode)
        {
            ProcessDebugInput(w, dbgCfg, ctx.BossQuery);
        }

        // ゲームオーバー時は自機の操作入力を停止：PlayerInputSystem 側でも同様に判定
        if (w.Has<GameOverTag>()) return;
    }

    private static void ResetCombat(World world, BattleInputSystemContext ctx)
    {
        // ゲームオーバー状態の解除：Remove は冪等のため事前の Has チェック不要
        world.Remove<GameOverTag>();

        BulletSpawner.ClearAllBossBulletsImmediate(world);
        MeleeSpawner.ClearAllBossMeleeImmediate(world);
        ctx.PlayerBulletsQuery.Each(CombatPoolService.Recycle);
        ctx.VfxQuery.Each(CombatPoolService.Recycle);
        CameraService.Reset();
        world.Set(new AimReticleState());

        ctx.BossQuery.Each(ResetBossEntity);
        ctx.PlayerEntitiesQuery.Each(ResetPlayerEntity);

        EventBus.Publish(new CombatResetEvent());
    }

    private static void RecycleEntity(Entity e) => CombatPoolService.Recycle(e);

    internal static void ResetBossEntity(Entity boss, ref Health hp, ref BossCombatState state, ref BossBrainComponent brainComp)
    {
        if (!boss.IsAlive()) return;

        var w = boss.CsWorld();
        var bossType = state.BossType;
        var (spawn, maxHp) = bossType switch {
            BossType.Boss1 => (w.Get<Boss1Config>().Spawn, w.Get<Boss1Config>().MaxHp),
            BossType.Boss2 => (w.Get<Boss2Config>().Spawn, w.Get<Boss2Config>().MaxHp),
            _ => (Vec2f.Zero, 1000f)
        };
        
        ref var trans = ref boss.GetMut<Transform>();
        trans.Position = spawn;
        trans.Rotation = 0.0f;

        boss.Set(new Velocity(Vec2f.Zero));

        hp.Current = maxHp;
        hp.Max = maxHp;

        brainComp.Brain.Reset(spawn);
        state.Phase = Phase.Phase1;
        state.CurrentSkillTag = StringId.Empty;
        state.StateTimer = 0.0f;
        state.TargetPosition = Vec2f.Zero;
        state.FacingRight = true;

        boss.Set(new GameplayTagState(brainComp.Brain.AbilitySystem.Tags.RawTags));
        boss.Set(new TimedEffectsState());

        // 一時状態・エフェクト・死亡タグの一括安全削除：Flecs ネイティブ API による冪等操作
        boss.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>();
        
        ref var sprite = ref boss.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
    }

    internal static void ResetPlayerEntity(Entity player)
    {
        if (!player.IsAlive()) return;

        var w = player.CsWorld();
        var type = player.Has<CharacterTypeComponent>()
            ? player.Get<CharacterTypeComponent>().Value
            : CharacterType.Cat;
        var cfg = w.GetPlayerConfig(type);

        player.Set(new Transform(cfg.Spawn, cfg.Scale));
        player.Set(new Velocity(Vec2f.Zero));
        player.Set(new Health(cfg.MaxHp));
        player.Set(new MovementIntent());
        player.Set(new WeaponIntent());
        player.Set(new DashState());
        player.Set(JumpCapability.FromPlayerConfig(cfg));
        player.Set(new GameplayTagState());
        player.Set(new TimedEffectsState());
        player.Set(new SpringScale(Vec2f.One, cfg.SpringFrequency, cfg.SpringDampingRatio));

        // 一時状態・エフェクト・死亡タグの一括安全削除：Flecs ネイティブ API による冪等操作
        player.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>();

        var weaponCfg = w.GetWeaponConfig(cfg.Weapon);
        if (weaponCfg is MeleeWeaponConfig meleeCfg)
        {
            player.Set(MeleeWeaponComponent.FromConfig(meleeCfg));
            var melee = player.Target<EquippedWeapon>();
            if (melee.IsAlive())
            {
                melee.Disable();
                melee.Set(new HitHistory());
            }
        }
        else if (weaponCfg is RangedWeaponConfig rangedCfg)
        {
            player.Set(RangedWeaponComponent.FromConfig(rangedCfg));
        }

        ref var sprite = ref player.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
        sprite.Scale = Vec2f.One;
        sprite.SpringOffsetY = 0.0f;
    }

    private static void ProcessDebugInput(World world, DebugRenderConfig dbgCfg, Query<Health, BossCombatState, BossBrainComponent> bossQuery)
    {
        var isF1 = GameEngine.CheckHitKey((int)InputButton.KeyF1) || GameEngine.CheckHitKey((int)InputButton.KeyH);
        if (isF1 && !_prevF1)
        {
            dbgCfg.IsActive = !dbgCfg.IsActive;
            dbgCfg.Enabled = dbgCfg.IsActive;
        }
        _prevF1 = isF1;

        if (!dbgCfg.IsActive) return;

        // 1/2/3/4 キーによる Boss およびキャラクターの迅速切替：InputAction 抽象経由
        ref readonly var flow = ref world.Get<GameFlow>();
        if (InputService.IsPressed(InputAction.SelectBoss1))
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss1);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectBoss2))
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss2);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectCharacter1))
        {
            GameManager.Current.SwitchToBattle(CharacterType.Cat, flow.SelectedBoss);
            return;
        }
        if (InputService.IsPressed(InputAction.SelectCharacter2))
        {
            GameManager.Current.SwitchToBattle(CharacterType.Bunny, flow.SelectedBoss);
            return;
        }

        // F5 キー：手触り検証用の空シーン（BossType.Empty）へ即時切替
        var isF5 = GameEngine.CheckHitKey((int)InputButton.KeyF5);
        if (isF5 && !_prevF5)
        {
            GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Empty);
            _prevF5 = isF5;
            return;
        }
        _prevF5 = isF5;

        // ゲームオーバー時はフェーズ切り替えを無効化します
        if (world.Has<GameOverTag>()) return;

        var isF2 = GameEngine.CheckHitKey((int)InputButton.KeyF2);
        var isF3 = GameEngine.CheckHitKey((int)InputButton.KeyF3);
        var isF4 = GameEngine.CheckHitKey((int)InputButton.KeyF4);

        if ((isF2 && !_prevF2) || (isF3 && !_prevF3) || (isF4 && !_prevF4))
        {
            _targetDebugPhase = (isF2 && !_prevF2) ? Phase.Phase1 : ((isF3 && !_prevF3) ? Phase.Phase2 : Phase.Phase3);
            bossQuery.Each(ApplyDebugPhase);
        }

        _prevF2 = isF2;
        _prevF3 = isF3;
        _prevF4 = isF4;
    }

    private static void ApplyDebugPhase(Entity boss, ref Health hp, ref BossCombatState state, ref BossBrainComponent brainComp)
    {
        if (!boss.IsAlive() || boss.Has<DeadTag>() || hp.Current <= 0.0f) return;
        brainComp.Brain.DebugSetPhase(_targetDebugPhase);

        var w = boss.CsWorld();
        var (p2, p3) = state.BossType switch {
            BossType.Boss1 => (w.Get<Boss1Config>().Phase2.HpThreshold, w.Get<Boss1Config>().Phase3.HpThreshold),
            BossType.Boss2 => (w.Get<Boss2Config>().Phase2.HpThreshold, w.Get<Boss2Config>().Phase3.HpThreshold),
            _ => (0.7f, 0.3f)
        };
        var ratio = _targetDebugPhase switch {
            Phase.Phase1 => 1.0f,
            Phase.Phase2 => p2,
            Phase.Phase3 => p3,
            _ => 1.0f
        };
        hp.Current = Math.Clamp((float)Math.Round(hp.Max * ratio), 1.0f, hp.Max);
        BulletSpawner.ClearAllBossBullets(w);
        MeleeSpawner.ClearAllBossMelee(w);
        state.Phase = _targetDebugPhase;
    }
}
