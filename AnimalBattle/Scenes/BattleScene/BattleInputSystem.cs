using AnimalBattle.Ai;
using AnimalBattle.Characters;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Framework.Pooling;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     謌ｦ髣倥す繝ｼ繝ｳ縺ｫ縺翫￠繧句・蜉帛女莉倥∬・讖滓桃菴懊√♀繧医・謌ｦ髣倥Μ繧ｻ繝・ヨ繝ｻ繧ｷ繝ｼ繝ｳ蠕ｩ蟶ｰ繧貞昇繧・ECS 繧ｷ繧ｹ繝・Β縲・///     GameState.Battle 迥ｶ諷区凾縺ｫ縺ｮ縺ｿ繧｢繧ｯ繝・ぅ繝悶→縺ｪ繧翫￣reUpdate 繝輔ぉ繝ｼ繧ｺ縺ｧ繝励Ξ繧､繝､繝ｼ縺ｮ蜈･蜉帙ｒ蜃ｦ逅・＠縺ｾ縺吶・/// </summary>
public static class BattleInputSystem
{
    private static bool _prevF1;
    private static bool _prevF2;
    private static bool _prevF3;
    private static bool _prevF4;
    private static bool _prevKey1;
    private static bool _prevKey2;
    private static bool _prevKey3;
    private static bool _prevKey4;

    private static Query _playerBulletsQuery;
    private static Query _vfxQuery;
    private static Query _playerEntitiesQuery;
    private static Query<Health, BossCombatState, BossBrainComponent> _bossQuery;
    private static Phase _targetDebugPhase;

    /// <summary>
    ///     謌ｦ髣伜・蜉帛・逅・す繧ｹ繝・Β繧・Flecs.NET 繝ｯ繝ｼ繝ｫ繝峨∈逋ｻ骭ｲ縺励∪縺吶・    /// </summary>
    /// <param name="world">蟇ｾ雎｡縺ｮ Flecs.NET 繝ｯ繝ｼ繝ｫ繝・/param>
    /// <returns>逋ｻ骭ｲ貂医∩繝ｯ繝ｼ繝ｫ繝・/returns>
    public static World AddBattleInputSystem(this World world)
    {
        _playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        _vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        _playerEntitiesQuery = world.QueryBuilder()
            .With<PlayerTag>()
            .Build();
        _bossQuery = world.QueryBuilder<Health, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Build();

        world.System("BattleInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteBattleInput);

        return world;
    }

    /// <summary>
    ///     謌ｦ髣倥す繝ｼ繝ｳ縺ｮ蜈･蜉帛女莉倥ヱ繧､繝励Λ繧､繝ｳ・亥錐蜑堺ｻ倥″髱咏噪繝｡繧ｽ繝・ラ・峨・    /// </summary>
    public static void ExecuteBattleInput(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        // ESC 繧ｭ繝ｼ縺ｧ繧ｿ繧､繝医Ν・城∈莠ｺ逕ｻ髱｢縺ｸ蠕ｩ蟶ｰ
        if (InputService.IsPressed(InputAction.Cancel))
        {
            GameManager.Current.SwitchToStart();
            return;
        }

        // R 繧ｭ繝ｼ縺ｧ謌ｦ髣倥Μ繧ｻ繝・ヨ
        if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCombat(w);
        }

        // デバッグ入力（F1 トグル、F2〜F4 フェーズ切替）の処理
        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;
        if (dbgCfg.DebugMode)
        {
            ProcessDebugInput(w, dbgCfg, _bossQuery);
        }

        // ゲームオーバー時は自機の操作入力を停止（PlayerInputSystem 側でも同様に判定）
        if (w.Has<GameOverTag>()) return;
    }

    private static void ResetCombat(World world)
    {
        if (world.Has<GameOverTag>())
        {
            world.Remove<GameOverTag>();
        }

        BulletSpawner.ClearAllBossBulletsImmediate(world);
        _playerBulletsQuery.Each(CombatPoolService.Recycle);
        _vfxQuery.Each(CombatPoolService.Recycle);
        CameraService.Reset();
        world.Set(new AimReticleState());

        _bossQuery.Each(ResetBossEntity);
        _playerEntitiesQuery.Each(ResetPlayerEntity);

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
        state.State = BossState.Unknown;
        state.CurrentSkillTag = StringId.Empty;
        state.StateTimer = 0.0f;
        state.TargetPosition = Vec2f.Zero;
        state.FacingRight = true;

        boss.Set(new GameplayTagState());
        boss.Set(new TimedEffectsState());

        if (boss.Has<HitstopState>()) boss.Remove<HitstopState>();
        if (boss.Has<SpriteJitter>()) boss.Remove<SpriteJitter>();
        if (boss.Has<IncomingDamage>()) boss.Remove<IncomingDamage>();
        
        ref var sprite = ref boss.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;

        if (boss.Has<DeadTag>()) boss.Remove<DeadTag>();
        if (boss.Has<HitFlashEffect>()) boss.Remove<HitFlashEffect>();
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
        player.Set(new InvincibleBlinkState());
        player.Set(new SpringScale(Vec2f.One, cfg.SpringFrequency, cfg.SpringDampingRatio));

        if (player.Has<HitstopState>()) player.Remove<HitstopState>();
        if (player.Has<SpriteJitter>()) player.Remove<SpriteJitter>();
        if (player.Has<IncomingDamage>()) player.Remove<IncomingDamage>();

        if (player.Has<CatWeaponState>())
        {
            player.Set(CatWeaponState.FromConfig(cfg));
        }
        if (player.Has<BunnyWeaponState>())
        {
            player.Set(BunnyWeaponState.FromConfig(cfg));
            var melee = player.Target<EquippedWeapon>();
            if (melee.IsAlive())
            {
                melee.Disable();
                melee.Set(new HitHistory());
                if (melee.Has<Sprite>()) melee.GetMut<Sprite>().IsVisible = true;
            }
        }
        
        player.GetMut<Animation>().Play("Idle");

        ref var sprite = ref player.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
        sprite.Scale = Vec2f.One;
        sprite.SpringOffsetY = 0.0f;

        if (player.Has<DeadTag>()) player.Remove<DeadTag>();
        if (player.Has<HitFlashEffect>()) player.Remove<HitFlashEffect>();
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

        // 1/2/3/4 繧ｭ繝ｼ縺ｫ繧医ｋ Boss 縺翫ｈ縺ｳ繧ｭ繝｣繝ｩ繧ｯ繧ｿ繝ｼ縺ｮ霑・溷・譖ｿ
        var isKey1 = GameEngine.CheckHitKey((int)InputButton.Key1);
        var isKey2 = GameEngine.CheckHitKey((int)InputButton.Key2);
        var isKey3 = GameEngine.CheckHitKey((int)InputButton.Key3);
        var isKey4 = GameEngine.CheckHitKey((int)InputButton.Key4);

        var justKey1 = isKey1 && !_prevKey1;
        var justKey2 = isKey2 && !_prevKey2;
        var justKey3 = isKey3 && !_prevKey3;
        var justKey4 = isKey4 && !_prevKey4;

        _prevKey1 = isKey1;
        _prevKey2 = isKey2;
        _prevKey3 = isKey3;
        _prevKey4 = isKey4;

        if (justKey1 || justKey2 || justKey3 || justKey4)
        {
            ref readonly var flow = ref world.Get<GameFlow>();
            if (justKey1)
            {
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss1);
                return;
            }
            if (justKey2)
            {
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss2);
                return;
            }
            if (justKey3)
            {
                GameManager.Current.SwitchToBattle(CharacterType.Cat, flow.SelectedBoss);
                return;
            }
            if (justKey4)
            {
                GameManager.Current.SwitchToBattle(CharacterType.Bunny, flow.SelectedBoss);
                return;
            }
        }

        // 繧ｲ繝ｼ繝繧ｪ繝ｼ繝舌・・・oss 謦・ｴ縺ｾ縺溘・閾ｪ讖滓ｭｻ莠｡・画凾縺ｯ繝輔ぉ繝ｼ繧ｺ蛻・崛繧堤┌蜉ｹ蛹・        if (world.Has<GameOverTag>()) return;

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
        state.Phase = _targetDebugPhase;
    }
}
