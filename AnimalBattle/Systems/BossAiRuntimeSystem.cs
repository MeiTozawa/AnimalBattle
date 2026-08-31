using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ecs;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss AI runtime を Flecs.NET の Query 境界で更新し、新旧の Boss component を同じ更新元で同期する system です。
/// </summary>
/// <remarks>
/// この system は弾物理と衝突より前に登録して使用します。プラグイン登録の置換は移行 task で行います。
/// </remarks>
public sealed class BossAiRuntimeSystem
{
    private readonly World _world;
    private readonly BossAiEcsBoundary _ecsBoundary;
    private readonly Query<Transform, Health> _playerQuery;
    private readonly Query<Transform, CollisionBox> _platformQuery;
    private readonly Query<Health, Boss1StateMachine, Boss1Stats> _boss1LegacyQuery;
    private readonly Query<Health, Boss2StateMachine, Boss2Stats> _boss2LegacyQuery;
    private readonly BossAiRuntime _runtime;
    private readonly Func<ulong, BossType, BossRuntimeSlot> _slotFactory;
    private readonly Func<BossEcsUpdateGate> _gateProvider;
    private readonly Dictionary<ulong, BossRuntimeSlot> _slots = new();
    private long _nextFrameUpdateNumber;

    /// <summary>
    /// Query 更新に必要な runtime と Entity ごとの事前確保済み slot の生成境界を指定します。
    /// </summary>
    /// <param name="world">Boss AI と Player component を持つ ECS World。</param>
    /// <param name="runtime">固定 module map と lifecycle を実行する共通 runtime。</param>
    /// <param name="slotFactory">Entity ごとの command port を接続した slot の生成関数。</param>
    /// <param name="gateProvider">初期化完了と runtime 準備完了を返す gate。</param>
    public BossAiRuntimeSystem(
        World world,
        BossAiRuntime runtime,
        Func<ulong, BossType, BossRuntimeSlot> slotFactory,
        Func<BossEcsUpdateGate> gateProvider)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(slotFactory);
        ArgumentNullException.ThrowIfNull(gateProvider);

        _world = world;
        _ecsBoundary = new BossAiEcsBoundary(world);
        _playerQuery = world.QueryBuilder<Transform, Health>()
            .With<PlayerTag>()
            .Build();
        _platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();
        _boss1LegacyQuery = world.QueryBuilder<Health, Boss1StateMachine, Boss1Stats>()
            .With<Boss1Tag>()
            .Build();
        _boss2LegacyQuery = world.QueryBuilder<Health, Boss2StateMachine, Boss2Stats>()
            .With<Boss2Tag>()
            .Build();
        _runtime = runtime;
        _slotFactory = slotFactory;
        _gateProvider = gateProvider;
    }

    /// <summary>
    /// 同一 Frame_Update の Player 観測を一回だけ取得して、全 Boss runtime slot を更新します。
    /// </summary>
    /// <param name="deltaTime">現在 frame の経過時間です。</param>
    /// <returns>Flecs Query 境界の更新結果です。</returns>
    public BossEcsBoundaryResult Update(float deltaTime)
    {
        BossEcsUpdateGate gate = _gateProvider();
        long frameUpdateNumber = _nextFrameUpdateNumber;
        if (!gate.IsOpen || !float.IsFinite(deltaTime) || deltaTime < 0.0f)
        {
            return _ecsBoundary.Update(
                in gate,
                frameUpdateNumber,
                (Entity ignoredEntity, ref BossAiData ignoredAi, ref BossMovementData ignoredMovement, ref BossAttackData ignoredAttack, ref BossPhaseTransitionData ignoredPhaseTransition, ref BossTerminalData ignoredTerminal) => { });
        }

        _nextFrameUpdateNumber++;
        PlayerObservation playerObservation = GetPlayerObservation(frameUpdateNumber);
        return _ecsBoundary.Update(
            in gate,
            frameUpdateNumber,
            (Entity entity, ref BossAiData ai, ref BossMovementData movement, ref BossAttackData attack, ref BossPhaseTransitionData phaseTransition, ref BossTerminalData terminal) =>
            {
                UpdateBoss(
                    entity,
                    frameUpdateNumber,
                    deltaTime,
                    in playerObservation,
                    ref ai,
                    ref movement,
                    ref attack,
                    ref phaseTransition,
                    ref terminal);
            });
    }

    private void UpdateBoss(
        Entity entity,
        long frameUpdateNumber,
        float deltaTime,
        in PlayerObservation playerObservation,
        ref BossAiData ai,
        ref BossMovementData movement,
        ref BossAttackData attack,
        ref BossPhaseTransitionData phaseTransition,
        ref BossTerminalData terminal)
    {
        if (ai.BossType is not (BossType.Boss1 or BossType.Boss2)
            || ai.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return;
        }

        ulong entityId = entity.Id;
        BossRuntimeSlot slot = GetOrCreateSlot(entityId, ai.BossType);
        if (!EnsureInitialized(entityId, slot, ai.BossType, ai.BossState))
        {
            return;
        }

        slot.SetDebugEnabled(ConfigManager.Render.Debug.Enabled);

        if (!GetHealth(entityId, ai.BossType, out float currentHealth, out float maximumHealth))
        {
            return;
        }

        if (entity.Has<Transform>())
        {
            ref readonly Transform bossTransform = ref entity.Get<Transform>();
            float vx = entity.Has<Velocity>() ? entity.Get<Velocity>().Vx : 0.0f;
            float vy = entity.Has<Velocity>() ? entity.Get<Velocity>().Vy : 0.0f;
            bool isGrounded = entity.Has<CollisionBox>() && entity.Get<CollisionBox>().IsGrounded;
            _runtime.SynchronizeBossState(ai.BossType, bossTransform.X, bossTransform.Y, vx, vy, isGrounded, playerObservation.IsAlive);
        }

        // Phase 判定を先に確定し、その後に state module が発行した Movement / Attack command を適用します。
        ErrorOr.ErrorOr<ErrorOr.Success> updateResult = _runtime.Update(
            slot,
            frameUpdateNumber,
            deltaTime,
            ai.Phase,
            in playerObservation,
            currentHp: ai.IsDead ? 0.0f : currentHealth,
            maxHp: maximumHealth);
        if (updateResult.IsError)
        {
            return;
        }

        // Phase -> Movement -> Attack の順で command を ECS data へ適用します。
        ApplyPhase(slot, ref phaseTransition, ref terminal);
        ApplyMovement(slot, ref movement);
        ApplyAttack(slot, ref attack);
        ai = slot.AiData;
        ai.UpdateSource = UpdateSource.NewRuntime;

        SynchronizeCompatibilityViews(entity, slot);
        SynchronizeLegacy(entityId, slot);

        // Movement / Attack / Effect commands を Flecs ECS ワールド（Transform, Velocity, 弾幕, 近接判定, 特效）へ適用します。
        BossCommandExecutor.ExecuteMovement(_world, entity, slot, deltaTime, _platformQuery);
        BossCommandExecutor.ExecuteAttack(_world, entity, slot);
        BossCommandExecutor.ExecuteEffects(_world, entity, slot);
    }

    private BossRuntimeSlot GetOrCreateSlot(ulong entityId, BossType bossType)
    {
        if (_slots.TryGetValue(entityId, out BossRuntimeSlot? slot))
        {
            return slot;
        }

        BossRuntimeSlot created = _slotFactory(entityId, bossType);
        if (created.EntityId != entityId || created.BossType != bossType)
        {
            throw new InvalidOperationException("Boss runtime slot の Entity または Boss 種別が Query 対象と一致しません。");
        }

        _slots.Add(entityId, created);
        return created;
    }

    private bool EnsureInitialized(ulong entityId, BossRuntimeSlot slot, BossType bossType, BossState queryState)
    {
        if (slot.InitializationComplete && slot.RuntimeReady)
        {
            return true;
        }

        bool imported = bossType switch
        {
            BossType.Boss1 => ImportBoss1(entityId, slot),
            BossType.Boss2 => ImportBoss2(entityId, slot),
            _ => false
        };
        if (!imported)
        {
            return false;
        }

        BossState initialState = slot.CurrentStateId.IsValid()
            ? slot.CurrentStateId
            : queryState;
        return initialState.IsValid() && !_runtime.Initialize(slot, initialState).IsError;
    }

    private bool ImportBoss1(ulong entityId, BossRuntimeSlot slot)
    {
        bool imported = false;
        _boss1LegacyQuery.Each((Entity entity, ref Health health, ref Boss1StateMachine stateMachine, ref Boss1Stats stats) =>
        {
            if (entity.Id != entityId || imported)
            {
                return;
            }

            imported = !MigrationAdapter.ImportBoss1(slot, in stateMachine, in stats, out _).IsError;
        });
        return imported;
    }

    private bool ImportBoss2(ulong entityId, BossRuntimeSlot slot)
    {
        bool imported = false;
        _boss2LegacyQuery.Each((Entity entity, ref Health health, ref Boss2StateMachine stateMachine, ref Boss2Stats stats) =>
        {
            if (entity.Id != entityId || imported)
            {
                return;
            }

            imported = !MigrationAdapter.ImportBoss2(slot, in stateMachine, in stats, out _).IsError;
        });
        return imported;
    }

    private static void SynchronizeCompatibilityViews(Entity entity, BossRuntimeSlot slot)
    {
        entity.Set(slot.CompatibilityView);
        if (slot.GetDebugData(out BossDebugData debugData))
        {
            entity.Set(debugData);
            return;
        }

        if (entity.Has<BossDebugData>())
        {
            entity.Remove<BossDebugData>();
        }
    }

    private void SynchronizeLegacy(ulong entityId, BossRuntimeSlot slot)
    {
        if (slot.BossType == BossType.Boss1)
        {
            _boss1LegacyQuery.Each((Entity entity, ref Health health, ref Boss1StateMachine stateMachine, ref Boss1Stats stats) =>
            {
                if (entity.Id != entityId)
                {
                    return;
                }

                MigrationResult legacy = MigrationAdapter.ConvertBoss1(in stateMachine, in stats);
                MigrationAdapter.SynchronizeFrame(
                    slot,
                    in legacy,
                    UpdateSource.NewRuntime,
                    out _,
                    out _);
                MigrationAdapter.ProjectBoss1(slot, ref stateMachine, ref stats, out _);
            });
            return;
        }

        _boss2LegacyQuery.Each((Entity entity, ref Health health, ref Boss2StateMachine stateMachine, ref Boss2Stats stats) =>
        {
            if (entity.Id != entityId)
            {
                return;
            }

            MigrationResult legacy = MigrationAdapter.ConvertBoss2(in stateMachine, in stats);
            MigrationAdapter.SynchronizeFrame(
                slot,
                in legacy,
                UpdateSource.NewRuntime,
                out _,
                out _);
            MigrationAdapter.ProjectBoss2(slot, ref stateMachine, ref stats, out _);
        });
    }

    private PlayerObservation GetPlayerObservation(long frameUpdateNumber)
    {
        PlayerObservation observation = new(1, false, 0.0f, 0.0f, 0.0f, 0.0f, frameUpdateNumber);
        _playerQuery.Each((Entity entity, ref Transform transform, ref Health health) =>
        {
            observation = new PlayerObservation(
                entity.Id,
                health.IsAlive,
                transform.X,
                transform.Y,
                0.0f,
                0.0f,
                frameUpdateNumber);
        });
        return observation;
    }

    private bool GetHealth(ulong entityId, BossType bossType, out float current, out float maximum)
    {
        float observedCurrent = 0.0f;
        float observedMaximum = 0.0f;
        bool found = false;
        if (bossType == BossType.Boss1)
        {
            _boss1LegacyQuery.Each((Entity entity, ref Health health, ref Boss1StateMachine stateMachine, ref Boss1Stats stats) =>
            {
                if (entity.Id != entityId)
                {
                    return;
                }

                observedCurrent = health.Current;
                observedMaximum = health.Max;
                found = true;
            });
            current = observedCurrent;
            maximum = observedMaximum;
            return found;
        }

        _boss2LegacyQuery.Each((Entity entity, ref Health health, ref Boss2StateMachine stateMachine, ref Boss2Stats stats) =>
        {
            if (entity.Id != entityId)
            {
                return;
            }

            observedCurrent = health.Current;
            observedMaximum = health.Max;
            found = true;
        });
        current = observedCurrent;
        maximum = observedMaximum;
        return found;
    }

    private static void ApplyPhase(BossRuntimeSlot slot, ref BossPhaseTransitionData phaseTransition, ref BossTerminalData terminal)
    {
        phaseTransition = slot.PhaseTransitionData;
        terminal = slot.TerminalData;
    }

    private void ApplyMovement(BossRuntimeSlot slot, ref BossMovementData movement)
    {
        IBossBehaviorState? behaviorState = _runtime.GetBehaviorState(slot.BossType);
        if (behaviorState is not null)
        {
            slot.MovementData.FacingRight = behaviorState.FacingRight;
        }

        movement = slot.MovementData;
        for (int index = 0; index < slot.MovementCommands.Count; index++)
        {
            MovementCommand command = slot.MovementCommands[index];
            switch (command.Kind)
            {
                case MovementCommandKind.SetVelocity:
                    movement.Velocity = new(command.X, command.Y);
                    break;
                case MovementCommandKind.MoveTo:
                    movement.TargetPosition = new(command.X, command.Y);
                    break;
                case MovementCommandKind.Stop:
                    movement.Velocity = new(0.0f, 0.0f);
                    break;
            }
        }
    }

    private static void ApplyAttack(BossRuntimeSlot slot, ref BossAttackData attack)
    {
        attack = slot.AttackData;
        for (int index = 0; index < slot.AttackCommands.Count; index++)
        {
            AttackCommand command = slot.AttackCommands[index];
            if (command.Kind == AttackCommandKind.ClearBossBullets)
            {
                attack.Stage = AttackStage.None;
                continue;
            }

            attack.AttackId = command.AttackId;
            attack.TargetPosition = new(command.X, command.Y);
            attack.Stage = AttackStage.Execute;
        }
    }
}
