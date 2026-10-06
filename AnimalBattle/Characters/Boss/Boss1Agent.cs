using System;
using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 1（Ancient Cat Overlord）専用自治エージェント（Pawn）。
///     空中浮遊機動、全方位弾幕、および急降下攻撃ライフサイクルを管理します。
/// </summary>
public sealed class Boss1Agent : BaseBossAgent
{
    private readonly Boss1BehaviorState _boss1State;

    /// <summary>
    ///     Boss 1 の Flecs エンティティおよびテクスチャハンドルを指定してエージェントを初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    public Boss1Agent(Entity entity, int spriteHandle)
        : this(entity, spriteHandle, CreateRuntimeAndSlot(entity.Id))
    {
    }

    private Boss1Agent(
        Entity entity,
        int spriteHandle,
        (BossAiRuntime Runtime, BossRuntimeSlot Slot, Boss1BehaviorState State) bundle)
        : base(entity, bundle.Runtime, bundle.Slot)
    {
        _boss1State = bundle.State;
        InitializeComponents(entity, spriteHandle);
    }

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b1Cfg = ConfigManager.Boss1;
        entity.Set(new Health(b1Cfg.MaxHp))
           .Set(new InvincibleOnHit(0.0f))
           .Set(new HitFlashConfig(0.08f, Color.White))
           .Set(new Transform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY))
           .Set(new Velocity(0.0f))
           .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
           .Set(new Hitbox(b1Cfg.GetHitboxShape(), b1Cfg.BodyContactDamage, Faction.Hostile, false))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
           .Set(new BossCombatState(IsVulnerable, Phase))
           .Add<BossTag>()
           .Add<Boss1Tag>();
    }


    /// <inheritdoc />
    public override BossSkill CurrentSkill => _boss1State.CurrentSkill != BossSkill.None
        ? _boss1State.CurrentSkill
        : Slot.AttackData.Skill;

    /// <inheritdoc />
    public override float StateTimer => _boss1State.StateElapsed;

    /// <inheritdoc />
    public override float HoverTimer => _boss1State.HoverTimer;

    /// <inheritdoc />
    public override bool IsVulnerable =>
        Slot.CurrentStateId == BossState.Boss1_Recovery && CurrentSkill == BossSkill.DiveRam;

    /// <inheritdoc />
    protected override float GetCurrentStateDuration()
    {
        var b1Cfg = ConfigManager.Boss1;
        return Slot.CurrentStateId.AsBoss1() switch {
            Boss1State.Hover => Phase switch {
                Phase.Phase2 => b1Cfg.Phase2.HoverDuration,
                Phase.Phase3 => b1Cfg.Phase3.HoverDuration,
                _ => b1Cfg.Phase1.HoverDuration
            },
            Boss1State.Telegraph => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var telegraphDef)
                ? telegraphDef.TelegraphDuration
                : 0.0f,
            Boss1State.Execute => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var execDef)
                ? execDef.ExecuteDuration
                : 0.0f,
            Boss1State.Recovery => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var recDef)
                ? recDef.RecoveryDuration
                : 0.0f,
            Boss1State.PhaseTransition => Phase switch {
                Phase.Phase2 => b1Cfg.Phase2.TransitionDuration,
                Phase.Phase3 => b1Cfg.Phase3.TransitionDuration,
                _ => b1Cfg.Phase1.TransitionDuration
            },
            _ => 0.0f
        };
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation =
            (float)(MathF.Sin(_boss1State.HoverTimer * ConfigManager.Render.BossEffects.BossHoverAngleSpeed) *
                ConfigManager.Render.BossEffects.BossHoverAngleAmp);
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b1Cfg = ConfigManager.Boss1;

        SetTransform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;

        ResetHealth(b1Cfg.MaxHp);

        _boss1State.Reset(b1Cfg.SpawnX, b1Cfg.SpawnY);

        var resetResult = Runtime.Reset(Slot, BossState.Boss1_Hover);
        if (resetResult.IsError)
            Console.WriteLine($"[Boss1Agent] Boss1 リセット失敗: {resetResult.FirstError.Description}");
    }

    private static (BossAiRuntime Runtime, BossRuntimeSlot Slot, Boss1BehaviorState State) CreateRuntimeAndSlot(
        ulong entityId)
    {
        var b1Cfg = ConfigManager.Boss1;
        var boss1State = new Boss1BehaviorState(b1Cfg.SpawnX, b1Cfg.SpawnY);
        var moduleMap = CreateBoss1ModuleMap(boss1State);
        TransitionService transitionService = new(moduleMap);
        var runtime = new BossAiRuntime(moduleMap, transitionService, type => boss1State);

        var configProvider = ConfigProvider.FromConfigManager();
        var movementCommands = new PreallocatedCommandBuffer<MovementCommand>(32);
        var attackCommands = new PreallocatedCommandBuffer<AttackCommand>(32);
        var effectCommands = new PreallocatedCommandBuffer<EffectCommand>(16);

        IMovementPort movement = new Boss1MovementPort(movementCommands);
        var slot = new BossRuntimeSlot(
            entityId,
            BossType.Boss1,
            configProvider,
            movement,
            new BufferedAttackPort(attackCommands),
            new BufferedEffectPort(effectCommands),
            new SystemRandomSource(),
            debugEnabled: ConfigManager.Render.Debug.Enabled,
            movementCommandBuffer: movementCommands,
            attackCommandBuffer: attackCommands,
            effectCommandBuffer: effectCommands);

        var initResult = runtime.Initialize(slot, BossState.Boss1_Hover);
        if (initResult.IsError)
            Console.WriteLine($"[Boss1Agent] Boss1 初期化失敗: {initResult.FirstError.Description}");

        return (runtime, slot, boss1State);
    }

    private static EnumModuleMap CreateBoss1ModuleMap(
        Boss1BehaviorState boss1State)
    {
        Boss1DefaultConfiguration boss1Config = new();

        IBossStateModule[] modules = [
            new HoverMovementModule(boss1Config, boss1State),
            new Boss1AttackSelectionModule(boss1Config, boss1State),
            new Boss1TelegraphModule(boss1Config, boss1State),
            new Boss1ExecuteModule(boss1Config, boss1State),
            new Boss1RecoveryModule(boss1Config, boss1State),
            new Boss1PhaseTransitionModule(boss1Config, boss1State),
            new Boss1DeadModule(boss1State)
        ];

        string[] dependencies = [
            "AttackPort",
            "Boss1Configuration",
            "EffectPort",
            "MovementPort",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies);
    }
}
