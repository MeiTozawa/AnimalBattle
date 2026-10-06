using System.Drawing;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 2（Ground Crusher）専用自治エージェント（Pawn）。
///     地上巡回追跡、足場間自律跳躍、および壁激突気絶ライフサイクルを管理します。
/// </summary>
public sealed class Boss2Agent : BaseBossAgent
{
    private readonly Boss2BehaviorState _boss2State;

    /// <summary>
    ///     Boss 2 の Flecs エンティティ、テクスチャハンドル、およびナビゲーショングラフプロバイダーを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">足場尋路用ナビゲーショングラフプロバイダー（省略可能）。</param>
    public Boss2Agent(
        Entity entity,
        int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
        : this(entity, spriteHandle, CreateRuntimeAndSlot(entity.Id, navGraphProvider))
    {
    }

    private Boss2Agent(
        Entity entity,
        int spriteHandle,
        (BossAiRuntime Runtime, BossRuntimeSlot Slot, Boss2BehaviorState State) bundle)
        : base(entity, bundle.Runtime, bundle.Slot)
    {
        _boss2State = bundle.State;
        InitializeComponents(entity, spriteHandle);
    }

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b2Cfg = ConfigManager.Boss2;
        entity.Set(new Health(b2Cfg.MaxHp))
           .Set(new InvincibleOnHit(0.0f))
           .Set(new HitFlashConfig(0.08f, Color.White))
           .Set(new Transform(b2Cfg.SpawnX, b2Cfg.SpawnY, b2Cfg.ScaleX, b2Cfg.ScaleY))
           .Set(new Velocity(0.0f))
           .Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight))
           .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
           .Set(new Hitbox(b2Cfg.GetHitboxShape(), b2Cfg.BodyContactDamage, Faction.Hostile, false))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
           .Set(new BossCombatState(IsVulnerable, Phase))
           .Add<BossTag>()
           .Add<Boss2Tag>();
    }

    /// <inheritdoc />
    public override BossSkill CurrentSkill => _boss2State.CurrentSkill != BossSkill.None
        ? _boss2State.CurrentSkill
        : Slot.AttackData.Skill;

    /// <inheritdoc />
    public override float StateTimer => _boss2State.StateElapsed;

    /// <inheritdoc />
    public override bool IsVulnerable =>
        Slot.CurrentStateId == BossState.Boss2_Recovery && CurrentSkill == BossSkill.LeapSmash;

    /// <inheritdoc />
    protected override float GetCurrentStateDuration()
    {
        var b2Cfg = ConfigManager.Boss2;
        return Slot.CurrentStateId.AsBoss2() switch {
            Boss2State.Idle => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.IdleDuration,
                Phase.Phase3 => b2Cfg.Phase3.IdleDuration,
                _ => b2Cfg.Phase1.IdleDuration
            },
            Boss2State.PatrolChase => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.ChaseTimeout,
                Phase.Phase3 => b2Cfg.Phase3.ChaseTimeout,
                _ => b2Cfg.Phase1.ChaseTimeout
            },
            Boss2State.MoveToTakeoff => b2Cfg.MoveToTakeoffTimeout,
            Boss2State.PlatformTraverse => b2Cfg.MaxAscentDuration + b2Cfg.LandingRecoveryDuration,
            Boss2State.Telegraph => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var telegraphDef)
                ? telegraphDef.TelegraphDuration
                : 0.0f,
            Boss2State.Execute => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var execDef)
                ? execDef.ExecuteDuration
                : 0.0f,
            Boss2State.WallCrashStun => b2Cfg.WallCrashStunDuration,
            Boss2State.Recovery => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var recDef)
                ? recDef.RecoveryDuration
                : 0.0f,
            Boss2State.PhaseTransition => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.TransitionDuration,
                Phase.Phase3 => b2Cfg.Phase3.TransitionDuration,
                _ => b2Cfg.Phase1.TransitionDuration
            },
            _ => 0.0f
        };
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
        ref var sprite = ref Entity.GetMut<Sprite>();
        sprite.FlipX = !Slot.MovementData.FacingRight;
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b2Cfg = ConfigManager.Boss2;

        SetTransform(b2Cfg.SpawnX, b2Cfg.SpawnY, b2Cfg.ScaleX, b2Cfg.ScaleY);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;

        ResetHealth(b2Cfg.MaxHp);

        ref var col = ref Entity.GetMut<CollisionBox>();
        col.IsGrounded = false;
        col.IsOnWall = false;

        _boss2State.Reset(b2Cfg.SpawnX, b2Cfg.SpawnY);

        var resetResult = Runtime.Reset(Slot, BossState.Boss2_Idle);
        if (resetResult.IsError)
            Console.WriteLine($"[Boss2Agent] Boss2 リセット失敗: {resetResult.FirstError.Description}");
    }

    private static (BossAiRuntime Runtime, BossRuntimeSlot Slot, Boss2BehaviorState State) CreateRuntimeAndSlot(
        ulong entityId,
        Func<PlatformNavigationGraph?>? navGraphProvider)
    {
        var b2Cfg = ConfigManager.Boss2;
        var boss2State = new Boss2BehaviorState(b2Cfg.SpawnX, b2Cfg.SpawnY);
        var moduleMap = CreateBoss2ModuleMap(boss2State, navGraphProvider);
        TransitionService transitionService = new(moduleMap);
        var runtime = new BossAiRuntime(moduleMap, transitionService, _ => boss2State);

        var configProvider = ConfigProvider.FromConfigManager();
        var movementCommands = new PreallocatedCommandBuffer<MovementCommand>(32);
        var attackCommands = new PreallocatedCommandBuffer<AttackCommand>(32);
        var effectCommands = new PreallocatedCommandBuffer<EffectCommand>(16);

        IMovementPort movement = new Boss2MovementPort(movementCommands);
        var slot = new BossRuntimeSlot(
            entityId,
            BossType.Boss2,
            configProvider,
            movement,
            new BufferedAttackPort(attackCommands),
            new BufferedEffectPort(effectCommands),
            new SystemRandomSource(),
            debugEnabled: ConfigManager.Render.Debug.Enabled,
            movementCommandBuffer: movementCommands,
            attackCommandBuffer: attackCommands,
            effectCommandBuffer: effectCommands);

        var initResult = runtime.Initialize(slot, BossState.Boss2_Idle);
        if (initResult.IsError)
            Console.WriteLine($"[Boss2Agent] Boss2 初期化失敗: {initResult.FirstError.Description}");

        return (runtime, slot, boss2State);
    }

    private static EnumModuleMap CreateBoss2ModuleMap(
        Boss2BehaviorState boss2State,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        Boss2DefaultConfiguration boss2Config = new();

        IBossStateModule[] modules = [
            new Boss2IdleModule(boss2State),
            new Boss2PatrolChaseModule(boss2Config, boss2State, navGraphProvider),
            new Boss2MoveToTakeoffModule(boss2Config, boss2State),
            new Boss2PlatformTraverseModule(BossState.Boss2_PlatformTraverse, TraverseMode.Jump, boss2Config,
                boss2State),
            new Boss2TelegraphModule(boss2Config, boss2State),
            new Boss2ExecuteModule(boss2Config, boss2State),
            new Boss2WallCrashStunModule(boss2Config, boss2State),
            new Boss2RecoveryModule(boss2Config, boss2State),
            new Boss2PhaseTransitionModule(boss2Config, boss2State),
            new Boss2DeadModule(boss2State)
        ];

        string[] dependencies = [
            "AttackPort",
            "Boss2Configuration",
            "EffectPort",
            "MovementPort",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies);
    }
}
