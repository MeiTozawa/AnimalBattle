using System.Collections.Frozen;

using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Components;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 旧 Boss component と新しい Boss AI runtime の値を変換する明示的な境界です。
/// </summary>
/// <remarks>
/// 取り込み後の source of truth は常に <see cref="BossAiData" /> と runtime slot です。
/// 旧 component は既存 system へ投影する出力としてだけ保持し、旧 enum の整数値を新しい
/// 状態 identity として保存しません。
/// </remarks>
public static class MigrationAdapter
{
    private static readonly FrozenDictionary<string, AttackId> _attackNames =
        new Dictionary<string, AttackId>(StringComparer.OrdinalIgnoreCase)
        {
            ["Aimed3Shot"] = Boss1AttackIds.Aimed3Shot,
            ["RingBurst"] = Boss1AttackIds.RingBurst,
            ["DoubleSpiral"] = Boss1AttackIds.DoubleSpiral,
            ["RainDrop"] = Boss1AttackIds.RainDrop,
            ["CrossChaos"] = Boss1AttackIds.CrossChaos,
            ["FastSniper"] = Boss1AttackIds.FastSniper,
            ["DiveRam"] = Boss1AttackIds.DiveRam,
            ["HeavySlash"] = Boss2AttackIds.HeavySlash,
            ["DoubleCleave"] = Boss2AttackIds.DoubleCleave,
            ["GroundStomp"] = Boss2AttackIds.GroundStomp,
            ["LeapSmash"] = Boss2AttackIds.LeapSmash,
            ["FrenzyCharge"] = Boss2AttackIds.FrenzyCharge,
            ["EarthquakeRoar"] = Boss2AttackIds.EarthquakeRoar
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>旧 component から Boss1 の新 runtime data を生成します。</summary>
    /// <param name="stateMachine">変換元の旧 Boss1 state machine。</param>
    /// <param name="stats">変換元の旧 Boss1 stats。</param>
    /// <returns>新 runtime data と変換診断。</returns>
    public static MigrationResult ConvertBoss1(
        in Boss1StateMachine stateMachine,
        in Boss1Stats stats)
    {
        AttackId attackId = ResolveAttackName(stateMachine.CurrentSkillName);
        bool attackRequired = stateMachine.CurrentState is Boss1StateKind.Telegraphing or Boss1StateKind.Casting;
        if (!MapBoss1State(stateMachine.CurrentState, attackId, out BossState stateId))
        {
            BossState safeState = attackRequired && !attackId.IsValid
                ? BossState.Boss1_AttackSelection
                : BossState.Boss1_Hover;
            AttackId safeAttackId = attackRequired && !attackId.IsValid
                ? AttackId.Unknown
                : attackId;
            ErrorCode errorCode = attackRequired && !attackId.IsValid
                ? ErrorCode.InvalidAttackId
                : ErrorCode.InvalidStateId;
            string reason = attackRequired && !attackId.IsValid
                ? "攻撃段階に必要な AttackId を解決できません。"
                : "固定 BossState に対応しない旧状態です。";
            return CreateSafeResult(
                BossType.Boss1,
                safeState,
                safeAttackId,
                ToSafePhase(stats.Phase),
                stateMachine.StateTimer,
                stateMachine.StateDuration,
                stateMachine.IsInvulnerable,
                stateMachine.CurrentSkillIndex,
                stateMachine.SubTimer,
                stateMachine.AngleOffset,
                new Vec2f(stateMachine.StartX, stateMachine.StartY),
                new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
                stateMachine.CurrentState,
                stats.BaseX,
                stats.BaseY,
                stats.HoverTimer,
                stats.HitFlashTimer,
                stats.AngleOffset,
                jumpCooldown: 0,
                actionSpeed: 0,
                facingRight: false,
                comboIndex: 0,
                errorCode,
                CreateConversionWarningMessage(
                    "Boss1",
                    stateMachine.CurrentState,
                    stateMachine.CurrentSkillName,
                    reason,
                    safeState,
                    safeAttackId));
        }

        if (attackRequired && !attackId.IsValid)
        {
            return CreateSafeResult(
                BossType.Boss1,
                BossState.Boss1_AttackSelection,
                AttackId.Unknown,
                ToSafePhase(stats.Phase),
                stateMachine.StateTimer,
                stateMachine.StateDuration,
                stateMachine.IsInvulnerable,
                stateMachine.CurrentSkillIndex,
                stateMachine.SubTimer,
                stateMachine.AngleOffset,
                new Vec2f(stateMachine.StartX, stateMachine.StartY),
                new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
                stateMachine.CurrentState,
                stats.BaseX,
                stats.BaseY,
                stats.HoverTimer,
                stats.HitFlashTimer,
                stats.AngleOffset,
                jumpCooldown: 0,
                actionSpeed: 0,
                facingRight: false,
                comboIndex: 0,
                ErrorCode.InvalidAttackId,
                CreateConversionWarningMessage(
                    "Boss1",
                    stateMachine.CurrentState,
                    stateMachine.CurrentSkillName,
                    "攻撃段階に必要な AttackId を解決できません。",
                    BossState.Boss1_AttackSelection,
                    AttackId.Unknown));
        }

        return BuildResult(
            BossType.Boss1,
            stateId,
            attackId,
            ToSafePhase(stats.Phase),
            stateMachine.StateTimer,
            stateMachine.StateDuration,
            stateMachine.IsInvulnerable,
            stateMachine.CurrentSkillIndex,
            stateMachine.SubTimer,
            stateMachine.AngleOffset,
            new Vec2f(stateMachine.StartX, stateMachine.StartY),
            new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
            stateMachine.CurrentState,
            stats.BaseX,
            stats.BaseY,
            stats.HoverTimer,
            stats.HitFlashTimer,
            stats.AngleOffset,
            jumpCooldown: 0,
            actionSpeed: 0,
            facingRight: false,
            comboIndex: 0);
    }

    /// <summary>旧 component から Boss2 の新 runtime data を生成します。</summary>
    /// <param name="stateMachine">変換元の旧 Boss2 state machine。</param>
    /// <param name="stats">変換元の旧 Boss2 stats。</param>
    /// <returns>新 runtime data と変換診断。</returns>
    public static MigrationResult ConvertBoss2(
        in Boss2StateMachine stateMachine,
        in Boss2Stats stats)
    {
        AttackId legacyAttackId = ResolveAttackName(stateMachine.CurrentSkillName);
        bool hasStateAttackId = ResolveBoss2StateAttack(stateMachine.CurrentState, out AttackId stateAttackId);
        AttackId attackId = hasStateAttackId ? stateAttackId : legacyAttackId;
        bool attackRequired = stateMachine.IsTelegraphing || stateMachine.IsAttacking;
        if (!MapBoss2State(stateMachine.CurrentState, out BossState stateId))
        {
            return CreateSafeResult(
                BossType.Boss2,
                BossState.Boss2_Idle,
                attackId,
                ToSafePhase(stats.Phase),
                stateMachine.StateTimer,
                stateMachine.StateDuration,
                stateMachine.IsInvulnerable,
                stateMachine.CurrentSkillIndex,
                stateMachine.SubTimer,
                angleOffset: 0,
                new Vec2f(stateMachine.StartX, stateMachine.StartY),
                new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
                stateMachine.CurrentState,
                stats.SpawnX,
                stats.SpawnY,
                stats.PatrolTimer,
                stats.HitFlashTimer,
                legacyBoss1StatsAngleOffset: 0,
                stats.JumpCooldown,
                stateMachine.ActionSpeed,
                stats.FacingRight,
                stats.ComboIndex,
                ErrorCode.InvalidStateId,
                CreateConversionWarningMessage(
                    "Boss2",
                    stateMachine.CurrentState,
                    stateMachine.CurrentSkillName,
                    "固定 BossState に対応しない旧状態です。",
                    BossState.Boss2_Idle,
                    attackId));
        }

        if (hasStateAttackId && legacyAttackId != stateAttackId)
        {
            return CreateSafeResult(
                BossType.Boss2,
                stateId,
                stateAttackId,
                ToSafePhase(stats.Phase),
                stateMachine.StateTimer,
                stateMachine.StateDuration,
                stateMachine.IsInvulnerable,
                stateMachine.CurrentSkillIndex,
                stateMachine.SubTimer,
                angleOffset: 0,
                new Vec2f(stateMachine.StartX, stateMachine.StartY),
                new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
                stateMachine.CurrentState,
                stats.SpawnX,
                stats.SpawnY,
                stats.PatrolTimer,
                stats.HitFlashTimer,
                legacyBoss1StatsAngleOffset: 0,
                stats.JumpCooldown,
                stateMachine.ActionSpeed,
                stats.FacingRight,
                stats.ComboIndex,
                ErrorCode.InvalidAttackId,
                CreateConversionWarningMessage(
                    "Boss2",
                    stateMachine.CurrentState,
                    stateMachine.CurrentSkillName,
                    "攻撃別の旧状態と AttackId が一致しないため、状態に対応する安全な AttackId を使用します。",
                    stateId,
                    stateAttackId));
        }

        if (attackRequired && !attackId.IsValid)
        {
            return CreateSafeResult(
                BossType.Boss2,
                BossState.Boss2_Idle,
                AttackId.Unknown,
                ToSafePhase(stats.Phase),
                stateMachine.StateTimer,
                stateMachine.StateDuration,
                stateMachine.IsInvulnerable,
                stateMachine.CurrentSkillIndex,
                stateMachine.SubTimer,
                angleOffset: 0,
                new Vec2f(stateMachine.StartX, stateMachine.StartY),
                new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
                stateMachine.CurrentState,
                stats.SpawnX,
                stats.SpawnY,
                stats.PatrolTimer,
                stats.HitFlashTimer,
                legacyBoss1StatsAngleOffset: 0,
                stats.JumpCooldown,
                stateMachine.ActionSpeed,
                stats.FacingRight,
                stats.ComboIndex,
                ErrorCode.InvalidAttackId,
                CreateConversionWarningMessage(
                    "Boss2",
                    stateMachine.CurrentState,
                    stateMachine.CurrentSkillName,
                    "攻撃段階に必要な AttackId を解決できません。",
                    BossState.Boss2_Idle,
                    AttackId.Unknown));
        }

        return BuildResult(
            BossType.Boss2,
            stateId,
            attackId,
            ToSafePhase(stats.Phase),
            stateMachine.StateTimer,
            stateMachine.StateDuration,
            stateMachine.IsInvulnerable,
            stateMachine.CurrentSkillIndex,
            stateMachine.SubTimer,
            angleOffset: 0,
            new Vec2f(stateMachine.StartX, stateMachine.StartY),
            new Vec2f(stateMachine.TargetX, stateMachine.TargetY),
            stateMachine.CurrentState,
            stats.SpawnX,
            stats.SpawnY,
            stats.PatrolTimer,
            stats.HitFlashTimer,
            legacyBoss1StatsAngleOffset: 0,
            stats.JumpCooldown,
            stateMachine.ActionSpeed,
            stats.FacingRight,
            stats.ComboIndex);
    }

    /// <summary>変換済み Boss1 data を runtime slot へ取り込みます。</summary>
    public static ErrorOr.ErrorOr<ErrorOr.Success> ImportBoss1(
        BossRuntimeSlot runtime,
        in Boss1StateMachine stateMachine,
        in Boss1Stats stats,
        out DiagnosticResult diagnostic)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        MigrationResult migration = ConvertBoss1(in stateMachine, in stats);
        if (!migration.IsSuccess)
        {
            diagnostic = migration.Diagnostic;
            return ErrorOr.Error.Failure("MigrationAdapter.ImportBoss1", diagnostic.Message);
        }

        ErrorOr.ErrorOr<ErrorOr.Success> applyResult = runtime.ApplyMigration(in migration);
        if (applyResult.IsError)
        {
            diagnostic = DiagnosticResult.Failure(
                DiagnosticSeverity.ExecutionError,
                ErrorCode.MissingDependency,
                "旧 Boss1 値を現在の runtime slot へ適用できません。",
                migration.AiData.BossState,
                migration.AttackData.AttackId,
                migration.AiData.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                BossState.Boss1_Hover);
            return ErrorOr.Error.Failure("MigrationAdapter.ImportBoss1", diagnostic.Message);
        }

        diagnostic = migration.Diagnostic.Severity == DiagnosticSeverity.Warning
            ? migration.Diagnostic
            : DiagnosticResult.Success("旧 Boss1 値を新 runtime へ移行しました。");
        return ErrorOr.Result.Success;
    }

    /// <summary>変換済み Boss2 data を runtime slot へ取り込みます。</summary>
    public static ErrorOr.ErrorOr<ErrorOr.Success> ImportBoss2(
        BossRuntimeSlot runtime,
        in Boss2StateMachine stateMachine,
        in Boss2Stats stats,
        out DiagnosticResult diagnostic)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        MigrationResult migration = ConvertBoss2(in stateMachine, in stats);
        if (!migration.IsSuccess)
        {
            diagnostic = migration.Diagnostic;
            return ErrorOr.Error.Failure("MigrationAdapter.ImportBoss2", diagnostic.Message);
        }

        ErrorOr.ErrorOr<ErrorOr.Success> applyResult = runtime.ApplyMigration(in migration);
        if (applyResult.IsError)
        {
            diagnostic = DiagnosticResult.Failure(
                DiagnosticSeverity.ExecutionError,
                ErrorCode.MissingDependency,
                "旧 Boss2 値を現在の runtime slot へ適用できません。",
                migration.AiData.BossState,
                migration.AttackData.AttackId,
                migration.AiData.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                BossState.Boss2_Idle);
            return ErrorOr.Error.Failure("MigrationAdapter.ImportBoss2", diagnostic.Message);
        }

        diagnostic = migration.Diagnostic.Severity == DiagnosticSeverity.Warning
            ? migration.Diagnostic
            : DiagnosticResult.Success("旧 Boss2 値を新 runtime へ移行しました。");
        return ErrorOr.Result.Success;
    }

    /// <summary>
    /// 新 runtime の純粋な ECS data から compatibility view を更新します。
    /// </summary>
    /// <param name="aiData">新 runtime が所有する共通 AI data。</param>
    /// <param name="movementData">新 runtime が所有する移動 data。</param>
    /// <param name="attackData">新 runtime が所有する攻撃 data。</param>
    /// <param name="currentView">維持する旧 component 向けの補助値を含む現在の view。</param>
    /// <returns>HUD、描画、衝突、再起動が参照する更新済み compatibility view。</returns>
    public static BossCompatibilityData CreateCompatibilityView(
        in BossAiData aiData,
        in BossMovementData movementData,
        in BossAttackData attackData,
        in BossCompatibilityData currentView)
    {
        BossCompatibilityData view = currentView;
        view.LegacyAttackValue = attackData.AttackId.Value;
        view.LegacyStateTimer = aiData.StateTimer;
        view.LegacyStateDuration = aiData.StateDuration;
        view.LegacyIsInvulnerable = aiData.IsInvulnerable;
        view.LegacyStartX = movementData.StartPosition.X;
        view.LegacyStartY = movementData.StartPosition.Y;
        view.LegacyTargetX = movementData.TargetPosition.X;
        view.LegacyTargetY = movementData.TargetPosition.Y;
        view.LegacyJumpCooldown = movementData.Cooldown;
        view.LegacyActionSpeed = movementData.Velocity.X;
        view.LegacyFacingRight = movementData.FacingRight;
        view.UpdateSource = aiData.UpdateSource == UpdateSource.Unknown
            ? UpdateSource.NewRuntime
            : aiData.UpdateSource;
        view.BossState = aiData.BossState;
        view.Phase = aiData.Phase;
        view.AttackId = attackData.AttackId;
        view.IsInvulnerable = aiData.IsInvulnerable;
        view.AttackStage = attackData.Stage;
        view.FrameUpdateNumber = aiData.FrameUpdateNumber;
        return view;
    }

    /// <summary>新 runtime の値を旧 Boss1 component へ互換投影します。</summary>
    public static ErrorOr.ErrorOr<ErrorOr.Success> ProjectBoss1(
        BossRuntimeSlot runtime,
        ref Boss1StateMachine stateMachine,
        ref Boss1Stats stats,
        out DiagnosticResult diagnostic)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runtime.BossType != BossType.Boss1 || !MapLegacyBoss1State(runtime.CurrentStateId, out Boss1StateKind state))
        {
            stateMachine = new Boss1StateMachine
            {
                CurrentState = Boss1StateKind.Hovering,
                CurrentSkillName = string.Empty
            };
            stats = new Boss1Stats
            {
                Phase = ToLegacyPhase(runtime.Phase)
            };
            diagnostic = DiagnosticResult.Warning(
                ErrorCode.InvalidStateId,
                $"新 runtime の Boss1 状態「{runtime.CurrentStateId}」を旧互換参照へ変換できません。理由: 旧 Boss1 状態に対応しません。影響: 当該状態と攻撃は旧参照へ表示しません。継続: Hovering の安全値で HUD、衝突、再起動の互換処理を継続します。",
                runtime.CurrentStateId,
                runtime.AttackData.AttackId,
                runtime.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                BossState.Boss1_Hover);
            return ErrorOr.Result.Success;
        }

        BossCompatibilityData compatibility = runtime.CompatibilityView;
        stateMachine.CurrentState = state;
        stateMachine.StateTimer = compatibility.LegacyStateTimer;
        stateMachine.StateDuration = compatibility.LegacyStateDuration;
        stateMachine.CurrentSkillIndex = compatibility.LegacySkillIndex;
        stateMachine.CurrentSkillName = GetLegacySkillName(compatibility.AttackId);
        stateMachine.SubTimer = compatibility.LegacySubTimer;
        stateMachine.AngleOffset = compatibility.LegacyAngleOffset;
        stateMachine.StartX = compatibility.LegacyStartX;
        stateMachine.StartY = compatibility.LegacyStartY;
        stateMachine.TargetX = compatibility.LegacyTargetX;
        stateMachine.TargetY = compatibility.LegacyTargetY;
        stateMachine.IsInvulnerable = compatibility.IsInvulnerable;

        stats.Phase = ToLegacyPhase(compatibility.Phase);
        stats.BaseX = compatibility.LegacyBaseOrSpawnX;
        stats.BaseY = compatibility.LegacyBaseOrSpawnY;
        stats.HoverTimer = compatibility.LegacyAuxiliaryTimer;
        stats.HitFlashTimer = compatibility.LegacyHitFlashTimer;
        stats.AngleOffset = compatibility.LegacyBoss1StatsAngleOffset;
        diagnostic = DiagnosticResult.Success("新 runtime の Boss1 値を互換 component へ投影しました。");
        return ErrorOr.Result.Success;
    }

    /// <summary>新 runtime の値を旧 Boss2 component へ互換投影します。</summary>
    public static ErrorOr.ErrorOr<ErrorOr.Success> ProjectBoss2(
        BossRuntimeSlot runtime,
        ref Boss2StateMachine stateMachine,
        ref Boss2Stats stats,
        out DiagnosticResult diagnostic)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runtime.BossType != BossType.Boss2 || !MapLegacyBoss2State(runtime.CurrentStateId, out Boss2StateKind state))
        {
            stateMachine = new Boss2StateMachine
            {
                CurrentState = Boss2StateKind.Idle,
                CurrentSkillName = string.Empty
            };
            stats = new Boss2Stats
            {
                Phase = ToLegacyPhase(runtime.Phase)
            };
            diagnostic = DiagnosticResult.Warning(
                ErrorCode.InvalidStateId,
                $"新 runtime の Boss2 状態「{runtime.CurrentStateId}」を旧互換参照へ変換できません。理由: 旧 Boss2 状態に対応しません。影響: 当該状態と攻撃は旧参照へ表示しません。継続: Idle の安全値で HUD、衝突、再起動の互換処理を継続します。",
                runtime.CurrentStateId,
                runtime.AttackData.AttackId,
                runtime.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                BossState.Boss2_Idle);
            return ErrorOr.Result.Success;
        }

        BossCompatibilityData compatibility = runtime.CompatibilityView;
        stateMachine.CurrentState = state;
        stateMachine.StateTimer = compatibility.LegacyStateTimer;
        stateMachine.StateDuration = compatibility.LegacyStateDuration;
        stateMachine.CurrentSkillIndex = compatibility.LegacySkillIndex;
        stateMachine.CurrentSkillName = GetLegacySkillName(compatibility.AttackId);
        stateMachine.SubTimer = compatibility.LegacySubTimer;
        stateMachine.StartX = compatibility.LegacyStartX;
        stateMachine.StartY = compatibility.LegacyStartY;
        stateMachine.TargetX = compatibility.LegacyTargetX;
        stateMachine.TargetY = compatibility.LegacyTargetY;
        stateMachine.ActionSpeed = compatibility.LegacyActionSpeed;
        stateMachine.IsInvulnerable = compatibility.IsInvulnerable;

        stats.Phase = ToLegacyPhase(compatibility.Phase);
        stats.SpawnX = compatibility.LegacyBaseOrSpawnX;
        stats.SpawnY = compatibility.LegacyBaseOrSpawnY;
        stats.FacingRight = compatibility.LegacyFacingRight;
        stats.PatrolTimer = compatibility.LegacyAuxiliaryTimer;
        stats.JumpCooldown = compatibility.LegacyJumpCooldown;
        stats.HitFlashTimer = compatibility.LegacyHitFlashTimer;
        stats.ComboIndex = compatibility.LegacyComboIndex;
        diagnostic = DiagnosticResult.Success("新 runtime の Boss2 値を互換 component へ投影しました。");
        return ErrorOr.Result.Success;
    }

    /// <summary>
    /// 新旧データを同時に扱う Frame_Update の更新元を新 runtime に固定し、値の不一致を診断します。
    /// </summary>
    /// <param name="runtime">新 runtime の source of truth。</param>
    /// <param name="legacyMigration">同じ frame に観測した旧 component の変換結果。</param>
    /// <param name="detectedSource">呼び出し元が判定した更新元。</param>
    /// <param name="hasMismatch">新旧値に不一致がある場合は true。</param>
    /// <param name="diagnostic">判定不能または不一致を表す日本語診断。</param>
    /// <returns>診断後も互換処理を継続できる場合は true。</returns>
    public static ErrorOr.ErrorOr<ErrorOr.Success> SynchronizeFrame(
        BossRuntimeSlot runtime,
        in MigrationResult legacyMigration,
        UpdateSource detectedSource,
        out bool hasMismatch,
        out DiagnosticResult diagnostic)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        hasMismatch = false;
        if (detectedSource == UpdateSource.Unknown
            || !Enum.IsDefined(detectedSource))
        {
            diagnostic = DiagnosticResult.Warning(
                ErrorCode.ExecutionFailed,
                $"Frame {runtime.FrameUpdateNumber} の Update_Source を判定できません。理由: 新旧の更新順を特定できません。影響: 旧値は書き戻しません。継続: 新 runtime を固定更新元として既存値で互換処理を継続します。",
                runtime.CurrentStateId,
                runtime.AttackData.AttackId,
                runtime.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                runtime.CurrentStateId);
            return ErrorOr.Result.Success;
        }

        hasMismatch = legacyMigration.BossType != runtime.BossType
            || legacyMigration.AiData.BossState != runtime.CurrentStateId
            || legacyMigration.AiData.Phase != runtime.Phase
            || legacyMigration.AttackData.AttackId != runtime.AttackData.AttackId
            || legacyMigration.AttackData.Stage != runtime.AttackData.Stage
            || legacyMigration.AiData.IsInvulnerable != runtime.AiData.IsInvulnerable;
        if (hasMismatch)
        {
            diagnostic = DiagnosticResult.Warning(
                ErrorCode.ExecutionFailed,
                $"Frame {runtime.FrameUpdateNumber} で新旧の Boss 値が不一致です。元値: 旧状態 {legacyMigration.AiData.BossState}、旧 AttackId {legacyMigration.AttackData.AttackId.Value}。理由: 固定更新元 {UpdateSource.NewRuntime} と旧 component の値が異なります。影響: 旧値は runtime を上書きしません。継続: 新 runtime の状態 {runtime.CurrentStateId}、AttackId {runtime.AttackData.AttackId.Value} で互換処理を継続します。",
                runtime.CurrentStateId,
                runtime.AttackData.AttackId,
                runtime.Phase,
                runtime.EntityId,
                runtime.FrameUpdateNumber,
                UpdateSource.NewRuntime,
                runtime.CurrentStateId);
            return ErrorOr.Result.Success;
        }

        diagnostic = DiagnosticResult.Success("新旧値は一致しています。Update_Source は新 runtime に固定します。");
        return ErrorOr.Result.Success;
    }

    /// <summary>移行結果と明示依存関係から StateContext を生成します。</summary>
    /// <param name="entityId">移行対象 Boss Entity の識別子。</param>
    /// <param name="migration">検証済みの移行結果。</param>
    /// <param name="configProvider">検証済み設定 snapshot の提供元。</param>
    /// <param name="configSnapshot">移行時に固定する検証済み設定 snapshot。</param>
    /// <param name="movement">移動 command の出力先。</param>
    /// <param name="attack">攻撃 command の出力先。</param>
    /// <param name="effect">演出 command の出力先。</param>
    /// <param name="observation">観測イベントの記録先。</param>
    /// <param name="random">明示的に注入する乱数源。</param>
    /// <param name="context">生成した State_Context。</param>
    /// <param name="diagnostic">生成の成否を示す診断。</param>
    /// <returns>依存関係と Entity 識別子が有効な場合は true。</returns>
    public static ErrorOr.ErrorOr<ErrorOr.Success> CreateContext(
        ulong entityId,
        in MigrationResult migration,
        IConfigProvider configProvider,
        BossConfigSnapshot configSnapshot,
        IMovementPort movement,
        IAttackPort attack,
        IEffectPort effect,
        IObservationService observation,
        IRandomSource random,
        out StateContext context,
        out DiagnosticResult diagnostic)
    {
        if (entityId == 0)
        {
            context = default;
            diagnostic = DiagnosticResult.Failure(
                DiagnosticSeverity.Warning,
                ErrorCode.MissingDependency,
                "移行用 State_Context の Entity 識別子が未指定です。",
                migration.AiData.BossState,
                migration.AttackData.AttackId,
                migration.AiData.Phase,
                entityId);
            return ErrorOr.Error.Validation("MigrationAdapter.CreateContext", "Entity 識別子が未指定です。");
        }

        if (!migration.IsSuccess)
        {
            context = default;
            diagnostic = migration.Diagnostic;
            return ErrorOr.Error.Failure("MigrationAdapter.CreateContext", diagnostic.Message);
        }

        ArgumentNullException.ThrowIfNull(configProvider);
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(random);
        if (!configSnapshot.IsValid)
        {
            context = default;
            diagnostic = DiagnosticResult.Failure(
                DiagnosticSeverity.Warning,
                ErrorCode.InvalidConfiguration,
                "移行用 State_Context の設定 snapshot が未検証です。",
                migration.AiData.BossState,
                migration.AttackData.AttackId,
                migration.AiData.Phase);
            return ErrorOr.Error.Validation("MigrationAdapter.CreateContext", "設定 snapshot が未検証です。");
        }

        context = new StateContext(
            entityId,
            migration.BossType,
            configProvider,
            configSnapshot,
            movement,
            attack,
            effect,
            observation,
            random);
        context.SetPhaseForTransition(migration.AiData.Phase);
        diagnostic = DiagnosticResult.Success("移行用 State_Context を生成しました。");
        return ErrorOr.Result.Success;
    }

    private static MigrationResult BuildResult(
        BossType bossType,
        BossState stateId,
        AttackId attackId,
        Phase phase,
        float stateTimer,
        float stateDuration,
        bool isInvulnerable,
        int skillIndex,
        float subTimer,
        float angleOffset,
        Vec2f startPosition,
        Vec2f targetPosition,
        Enum legacyState,
        float baseOrSpawnX,
        float baseOrSpawnY,
        float auxiliaryTimer,
        float hitFlashTimer,
        float legacyBoss1StatsAngleOffset,
        float jumpCooldown,
        float actionSpeed,
        bool facingRight,
        int comboIndex)
    {
        AttackStage stage = GetAttackStage(stateId);
        BossAiData aiData = new(bossType, stateId, phase)
        {
            StateTimer = stateTimer,
            StateDuration = stateDuration,
            IsInvulnerable = isInvulnerable,
            IsDead = stateId is BossState.Boss1_Dead or BossState.Boss2_Dead,
            AttackGenerationEnabled = stateId is not (BossState.Boss1_Dead or BossState.Boss2_Dead),
            UpdateSource = UpdateSource.NewRuntime
        };
        BossAttackData attackData = new(attackId, stage)
        {
            AttackIndex = skillIndex,
            SubTimer = subTimer,
            AngleOffset = angleOffset,
            TargetPosition = targetPosition,
            ConfiguredDuration = stateDuration,
            ElapsedDuration = stateTimer
        };
        BossMovementData movementData = new(stateId)
        {
            StartPosition = startPosition,
            TargetPosition = targetPosition,
            Velocity = bossType == BossType.Boss2
                ? new Vec2f(facingRight ? actionSpeed : -actionSpeed, 0)
                : Vec2f.Zero,
            FacingRight = facingRight,
            Cooldown = jumpCooldown,
            IsAirborne = IsAirborne(stateId),
            IsGrounded = bossType == BossType.Boss2 && !IsAirborne(stateId)
        };
        BossCompatibilityData compatibility = new()
        {
            LegacyStateValue = Convert.ToInt32(legacyState),
            LegacyAttackValue = attackId.Value,
            LegacyStateTimer = stateTimer,
            LegacyStateDuration = stateDuration,
            LegacyIsInvulnerable = isInvulnerable,
            LegacySkillIndex = skillIndex,
            LegacySubTimer = subTimer,
            LegacyAngleOffset = angleOffset,
            LegacyStartX = startPosition.X,
            LegacyStartY = startPosition.Y,
            LegacyTargetX = targetPosition.X,
            LegacyTargetY = targetPosition.Y,
            LegacyBaseOrSpawnX = baseOrSpawnX,
            LegacyBaseOrSpawnY = baseOrSpawnY,
            LegacyAuxiliaryTimer = auxiliaryTimer,
            LegacyHitFlashTimer = hitFlashTimer,
            LegacyBoss1StatsAngleOffset = legacyBoss1StatsAngleOffset,
            LegacyJumpCooldown = jumpCooldown,
            LegacyActionSpeed = actionSpeed,
            LegacyFacingRight = facingRight,
            LegacyComboIndex = comboIndex,
            UpdateSource = UpdateSource.NewRuntime,
            BossState = stateId,
            Phase = phase,
            AttackId = attackId,
            TransitionReason = new(ReasonKind.ExternalRequest, attackId: attackId, originalInput: Convert.ToInt32(legacyState)),
            IsInvulnerable = isInvulnerable,
            AttackStage = stage,
            HasDebugData = false,
            FrameUpdateNumber = 0
        };

        return new MigrationResult(
            bossType,
            0,
            aiData,
            movementData,
            attackData,
            compatibility,
            DiagnosticResult.Success("旧 Boss component を新 runtime data へ変換しました。"));
    }

    /// <summary>旧値の変換不能部分を安全値へ置き換えた移行結果を生成します。</summary>
    private static MigrationResult CreateSafeResult(
        BossType bossType,
        BossState safeState,
        AttackId safeAttackId,
        Phase phase,
        float stateTimer,
        float stateDuration,
        bool isInvulnerable,
        int skillIndex,
        float subTimer,
        float angleOffset,
        Vec2f startPosition,
        Vec2f targetPosition,
        Enum legacyState,
        float baseOrSpawnX,
        float baseOrSpawnY,
        float auxiliaryTimer,
        float hitFlashTimer,
        float legacyBoss1StatsAngleOffset,
        float jumpCooldown,
        float actionSpeed,
        bool facingRight,
        int comboIndex,
        ErrorCode errorCode,
        string message)
    {
        MigrationResult result = BuildResult(
            bossType,
            safeState,
            safeAttackId,
            phase,
            stateTimer,
            stateDuration,
            isInvulnerable,
            skillIndex,
            subTimer,
            angleOffset,
            startPosition,
            targetPosition,
            legacyState,
            baseOrSpawnX,
            baseOrSpawnY,
            auxiliaryTimer,
            hitFlashTimer,
            legacyBoss1StatsAngleOffset,
            jumpCooldown,
            actionSpeed,
            facingRight,
            comboIndex);
        DiagnosticResult diagnostic = DiagnosticResult.Warning(
            errorCode,
            message,
            safeState,
            safeAttackId,
            phase,
            updateSource: UpdateSource.NewRuntime,
            recoveryBossState: safeState);
        return new MigrationResult(
            result.BossType,
            result.EntityId,
            result.AiData,
            result.MovementData,
            result.AttackData,
            result.CompatibilityData,
            diagnostic);
    }

    private static string CreateConversionWarningMessage(
        string bossName,
        Enum legacyState,
        string? legacyAttackName,
        string reason,
        BossState safeState,
        AttackId safeAttackId)
    {
        string attackName = string.IsNullOrWhiteSpace(legacyAttackName) ? "未指定" : legacyAttackName;
        return $"旧 {bossName} の元値: 状態「{legacyState}」(値:{Convert.ToInt32(legacyState)})、攻撃「{attackName}」を安全値へ変換しました。理由: {reason} 影響: 変換不能な状態または攻撃は実行しません。継続: 状態 {safeState}、AttackId {safeAttackId.Value} で処理を継続します。";
    }

    private static MigrationResult CreateFailure(
        BossType bossType,
        ErrorCode errorCode,
        string message,
        BossState recoveryState,
        AttackId attackId,
        Phase phase,
        BossState stateId = BossState.Unknown)
    {
        DiagnosticResult diagnostic = DiagnosticResult.Failure(
            DiagnosticSeverity.ExecutionError,
            errorCode,
            message,
            stateId,
            attackId,
            phase,
            recoveryBossState: recoveryState);
        return new MigrationResult(bossType, 0, default, default, default, default, diagnostic);
    }

    private static AttackId ResolveAttackName(string? name)
        => !string.IsNullOrWhiteSpace(name) && _attackNames.TryGetValue(name, out AttackId attackId)
            ? attackId
            : AttackId.Unknown;

    private static string GetLegacySkillName(AttackId attackId)
    {
        return attackId.Value switch
        {
            1 => "Aimed3Shot",
            2 => "RingBurst",
            3 => "DoubleSpiral",
            4 => "RainDrop",
            5 => "CrossChaos",
            6 => "FastSniper",
            7 => "DiveRam",
            201 => "HeavySlash",
            202 => "DoubleCleave",
            203 => "GroundStomp",
            204 => "LeapSmash",
            205 => "FrenzyCharge",
            206 => "EarthquakeRoar",
            _ => string.Empty
        };
    }

    private static Phase ToPhase(int phase)
        => phase switch
        {
            1 => Phase.Phase1,
            2 => Phase.Phase2,
            3 => Phase.Phase3,
            _ => Phase.Unknown
        };

    private static Phase ToSafePhase(int phase)
    {
        Phase convertedPhase = ToPhase(phase);
        return convertedPhase == Phase.Unknown ? Phase.Phase1 : convertedPhase;
    }

    private static int ToLegacyPhase(Phase phase)
        => phase switch
        {
            Phase.Phase2 => 2,
            Phase.Phase3 => 3,
            _ => 1
        };

    private static AttackStage GetAttackStage(BossState state)
        => state switch
        {
            BossState.Boss1_Telegraph or BossState.Boss2_HeavySlashTelegraph
                or BossState.Boss2_DoubleCleaveTelegraph or BossState.Boss2_GroundStompTelegraph
                or BossState.Boss2_LeapSmashTelegraph or BossState.Boss2_FrenzyChargeTelegraph
                or BossState.Boss2_EarthquakeRoarTelegraph => AttackStage.Telegraph,
            BossState.Boss1_Aimed3ShotExecute or BossState.Boss1_RingBurstExecute
                or BossState.Boss1_DoubleSpiralExecute or BossState.Boss1_RainDropExecute
                or BossState.Boss1_CrossChaosExecute or BossState.Boss1_FastSniperExecute
                or BossState.Boss1_DiveRamExecute or BossState.Boss2_HeavySlashExecute
                or BossState.Boss2_DoubleCleaveExecute or BossState.Boss2_GroundStompExecute
                or BossState.Boss2_LeapSmashAscent or BossState.Boss2_LeapSmashDescent
                or BossState.Boss2_FrenzyChargeExecute or BossState.Boss2_EarthquakeRoarExecute => AttackStage.Execute,
            BossState.Boss1_Recovery or BossState.Boss2_Recovery => AttackStage.Recovery,
            _ => AttackStage.None
        };

    private static bool MapBoss1State(Boss1StateKind legacyState, AttackId attackId, out BossState stateId)
    {
        stateId = legacyState switch
        {
            Boss1StateKind.Hovering => BossState.Boss1_Hover,
            Boss1StateKind.Telegraphing => BossState.Boss1_Telegraph,
            Boss1StateKind.Casting => ResolveBoss1Execute(attackId),
            Boss1StateKind.Recovering => BossState.Boss1_Recovery,
            Boss1StateKind.PhaseTransition => BossState.Boss1_PhaseTransition,
            _ => BossState.Unknown
        };
        return stateId.IsValid();
    }

    private static bool ResolveBoss2StateAttack(Boss2StateKind legacyState, out AttackId attackId)
    {
        attackId = legacyState switch
        {
            Boss2StateKind.TelegraphHeavySlash or Boss2StateKind.ExecutingHeavySlash => Boss2AttackIds.HeavySlash,
            Boss2StateKind.TelegraphDoubleCleave or Boss2StateKind.ExecutingDoubleCleave => Boss2AttackIds.DoubleCleave,
            Boss2StateKind.TelegraphGroundStomp or Boss2StateKind.ExecutingGroundStomp => Boss2AttackIds.GroundStomp,
            Boss2StateKind.TelegraphLeapSmash or Boss2StateKind.ExecutingLeapAscent or Boss2StateKind.ExecutingLeapDescent => Boss2AttackIds.LeapSmash,
            Boss2StateKind.TelegraphFrenzyCharge or Boss2StateKind.ExecutingFrenzyCharge => Boss2AttackIds.FrenzyCharge,
            Boss2StateKind.TelegraphEarthquakeRoar or Boss2StateKind.ExecutingEarthquakeRoar => Boss2AttackIds.EarthquakeRoar,
            _ => AttackId.Unknown
        };
        return attackId.IsValid;
    }

    private static bool MapBoss2State(Boss2StateKind legacyState, out BossState stateId)
    {
        stateId = legacyState switch
        {
            Boss2StateKind.Idle => BossState.Boss2_Idle,
            Boss2StateKind.PatrolChase => BossState.Boss2_PatrolChase,
            Boss2StateKind.MoveToTakeoff => BossState.Boss2_MoveToTakeoff,
            Boss2StateKind.PlatformJumping => BossState.Boss2_PlatformJump,
            Boss2StateKind.PlatformFalling => BossState.Boss2_PlatformFall,
            Boss2StateKind.LandingRecovery => BossState.Boss2_LandingRecovery,
            Boss2StateKind.TelegraphHeavySlash => BossState.Boss2_HeavySlashTelegraph,
            Boss2StateKind.TelegraphDoubleCleave => BossState.Boss2_DoubleCleaveTelegraph,
            Boss2StateKind.TelegraphGroundStomp => BossState.Boss2_GroundStompTelegraph,
            Boss2StateKind.TelegraphLeapSmash => BossState.Boss2_LeapSmashTelegraph,
            Boss2StateKind.TelegraphFrenzyCharge => BossState.Boss2_FrenzyChargeTelegraph,
            Boss2StateKind.TelegraphEarthquakeRoar => BossState.Boss2_EarthquakeRoarTelegraph,
            Boss2StateKind.ExecutingHeavySlash => BossState.Boss2_HeavySlashExecute,
            Boss2StateKind.ExecutingDoubleCleave => BossState.Boss2_DoubleCleaveExecute,
            Boss2StateKind.ExecutingGroundStomp => BossState.Boss2_GroundStompExecute,
            Boss2StateKind.ExecutingLeapAscent => BossState.Boss2_LeapSmashAscent,
            Boss2StateKind.ExecutingLeapDescent => BossState.Boss2_LeapSmashDescent,
            Boss2StateKind.ExecutingFrenzyCharge => BossState.Boss2_FrenzyChargeExecute,
            Boss2StateKind.ExecutingEarthquakeRoar => BossState.Boss2_EarthquakeRoarExecute,
            Boss2StateKind.WallCrashStun => BossState.Boss2_WallCrashStun,
            Boss2StateKind.SkillRecovery => BossState.Boss2_Recovery,
            Boss2StateKind.PhaseTransition => BossState.Boss2_PhaseTransition,
            Boss2StateKind.Dead => BossState.Boss2_Dead,
            _ => BossState.Unknown
        };
        return stateId.IsValid();
    }

    private static bool MapLegacyBoss1State(BossState stateId, out Boss1StateKind state)
    {
        state = stateId switch
        {
            BossState.Boss1_Hover or BossState.Boss1_AttackSelection => Boss1StateKind.Hovering,
            BossState.Boss1_Telegraph => Boss1StateKind.Telegraphing,
            BossState.Boss1_Aimed3ShotExecute or BossState.Boss1_RingBurstExecute
                or BossState.Boss1_DoubleSpiralExecute or BossState.Boss1_RainDropExecute
                or BossState.Boss1_CrossChaosExecute or BossState.Boss1_FastSniperExecute
                or BossState.Boss1_DiveRamExecute => Boss1StateKind.Casting,
            BossState.Boss1_Recovery => Boss1StateKind.Recovering,
            BossState.Boss1_PhaseTransition or BossState.Boss1_Dead => Boss1StateKind.PhaseTransition,
            _ => default
        };
        return stateId is >= BossState.Boss1_Hover and <= BossState.Boss1_Dead;
    }

    private static bool MapLegacyBoss2State(BossState stateId, out Boss2StateKind state)
    {
        state = stateId switch
        {
            BossState.Boss2_Idle => Boss2StateKind.Idle,
            BossState.Boss2_PatrolChase => Boss2StateKind.PatrolChase,
            BossState.Boss2_MoveToTakeoff => Boss2StateKind.MoveToTakeoff,
            BossState.Boss2_PlatformJump => Boss2StateKind.PlatformJumping,
            BossState.Boss2_PlatformFall => Boss2StateKind.PlatformFalling,
            BossState.Boss2_LandingRecovery => Boss2StateKind.LandingRecovery,
            BossState.Boss2_HeavySlashTelegraph => Boss2StateKind.TelegraphHeavySlash,
            BossState.Boss2_DoubleCleaveTelegraph => Boss2StateKind.TelegraphDoubleCleave,
            BossState.Boss2_GroundStompTelegraph => Boss2StateKind.TelegraphGroundStomp,
            BossState.Boss2_LeapSmashTelegraph => Boss2StateKind.TelegraphLeapSmash,
            BossState.Boss2_FrenzyChargeTelegraph => Boss2StateKind.TelegraphFrenzyCharge,
            BossState.Boss2_EarthquakeRoarTelegraph => Boss2StateKind.TelegraphEarthquakeRoar,
            BossState.Boss2_HeavySlashExecute => Boss2StateKind.ExecutingHeavySlash,
            BossState.Boss2_DoubleCleaveExecute => Boss2StateKind.ExecutingDoubleCleave,
            BossState.Boss2_GroundStompExecute => Boss2StateKind.ExecutingGroundStomp,
            BossState.Boss2_LeapSmashAscent => Boss2StateKind.ExecutingLeapAscent,
            BossState.Boss2_LeapSmashDescent => Boss2StateKind.ExecutingLeapDescent,
            BossState.Boss2_FrenzyChargeExecute => Boss2StateKind.ExecutingFrenzyCharge,
            BossState.Boss2_EarthquakeRoarExecute => Boss2StateKind.ExecutingEarthquakeRoar,
            BossState.Boss2_WallCrashStun => Boss2StateKind.WallCrashStun,
            BossState.Boss2_Recovery => Boss2StateKind.SkillRecovery,
            BossState.Boss2_PhaseTransition => Boss2StateKind.PhaseTransition,
            BossState.Boss2_Dead => Boss2StateKind.Dead,
            _ => default
        };
        return stateId is >= BossState.Boss2_Idle and <= BossState.Boss2_Dead;
    }

    private static BossState ResolveBoss1Execute(AttackId attackId)
        => Boss1StateIds.GetExecuteState(attackId);

    private static bool IsAirborne(BossState stateId)
        => stateId is BossState.Boss2_PlatformJump or BossState.Boss2_PlatformFall
            or BossState.Boss2_LeapSmashAscent or BossState.Boss2_LeapSmashDescent;
}

/// <summary>旧値を新 runtime data へ変換した結果です。</summary>
public readonly struct MigrationResult
{
    /// <summary>対象 Boss 種別です。</summary>
    public BossType BossType { get; }
    /// <summary>移行対象 Entity 識別子です。純粋変換では 0 です。</summary>
    public ulong EntityId { get; }
    /// <summary>新 runtime の共通 AI data です。</summary>
    public BossAiData AiData { get; }
    /// <summary>新 runtime の移動 data です。</summary>
    public BossMovementData MovementData { get; }
    /// <summary>新 runtime の攻撃 data です。</summary>
    public BossAttackData AttackData { get; }
    /// <summary>旧 system 向け互換 view の data です。</summary>
    public BossCompatibilityData CompatibilityData { get; }
    /// <summary>変換診断です。</summary>
    public DiagnosticResult Diagnostic { get; }
    /// <summary>変換が成功したかどうかを取得します。</summary>
    public bool IsSuccess => Diagnostic.IsSuccess;

    /// <summary>移行結果を生成します。</summary>
    public MigrationResult(
        BossType bossType,
        ulong entityId,
        BossAiData aiData,
        BossMovementData movementData,
        BossAttackData attackData,
        BossCompatibilityData compatibilityData,
        DiagnosticResult diagnostic)
    {
        BossType = bossType;
        EntityId = entityId;
        AiData = aiData;
        MovementData = movementData;
        AttackData = attackData;
        CompatibilityData = compatibilityData;
        Diagnostic = diagnostic;
    }
}
