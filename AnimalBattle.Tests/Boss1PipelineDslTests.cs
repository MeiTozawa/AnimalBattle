using System;
using System.Collections.Generic;
using System.Drawing;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using Xunit;

namespace AnimalBattle.Tests;

/// <summary>
/// Boss1 の声明的 DSL 流水線（Fluent API）の構築と設定整合性を検証するテストです。
/// </summary>
public sealed class Boss1PipelineDslTests
{
    /// <summary>
    /// Boss1PipelineDefinition から構築されたパイプラインが全 3 フェーズと全 7 技能を正しく包含していることを検証します。
    /// </summary>
    [Fact]
    public void Boss1Pipeline_BuildsAllPhasesAndAttacksCorrectly()
    {
        BossBehaviorPipeline pipeline = Boss1PipelineDefinition.Pipeline;

        Assert.Equal(BossType.Boss1, pipeline.BossType);
        Assert.Equal(ConfigManager.Boss1.BossName, pipeline.Name);

        // 3 フェーズの確認
        Assert.NotEmpty(pipeline.GetRules(Phase.Phase1));
        Assert.NotEmpty(pipeline.GetRules(Phase.Phase2));
        Assert.NotEmpty(pipeline.GetRules(Phase.Phase3));

        // 7 つの攻撃定義の確認
        Assert.Equal(7, pipeline.Attacks.Count);
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.Aimed3Shot));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.DiveRam));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.RingBurst));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.DoubleSpiral));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.RainDrop));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.CrossChaos));
        Assert.True(pipeline.Attacks.ContainsKey(BossSkill.FastSniper));
    }

    /// <summary>
    /// Boss1DefaultConfiguration が Boss1PipelineDefinition.Pipeline と正しく連動していることを検証します。
    /// </summary>
    [Fact]
    public void Boss1DefaultConfiguration_IntegratesWithPipeline()
    {
        Boss1DefaultConfiguration config = new();

        Assert.True(config.GetAttack(BossSkill.Aimed3Shot, out Boss1AttackPattern aimed));
        Assert.True(aimed.IsValid);
        Assert.Equal(0.40f, aimed.Definition.TelegraphDuration);
        Assert.Equal(0.90f, aimed.Definition.ExecuteDuration);
        Assert.Equal(1.20f, aimed.Definition.RecoveryDuration);
        Assert.Equal(372.0f, aimed.BulletSpeed);

        Assert.True(config.GetAttack(BossSkill.DiveRam, out Boss1AttackPattern dive));
        Assert.True(dive.IsValid);
        Assert.Equal(0.55f, dive.Definition.TelegraphDuration);
        Assert.Equal(0.45f, dive.Definition.ExecuteDuration);
        Assert.Equal(1.40f, dive.Definition.RecoveryDuration);
        Assert.Equal(960.0f, dive.DashSpeed);

        Boss1PhaseSettings phase1 = config.GetPhase(Phase.Phase1);
        Assert.Equal(0.8f, phase1.HoverDuration);
        Assert.Contains(BossSkill.Aimed3Shot, phase1.AttackOrder);
        Assert.Contains(BossSkill.DiveRam, phase1.AttackOrder);
        Assert.Contains(BossSkill.RingBurst, phase1.AttackOrder);
    }

    private sealed class Boss1MockMovementPort : IMovementPort
    {
        public ErrorOr.ErrorOr<ErrorOr.Success> MoveTo(float x, float y) => ErrorOr.Result.Success;
        public ErrorOr.ErrorOr<ErrorOr.Success> SetVelocity(float vx, float vy) => ErrorOr.Result.Success;
        public ErrorOr.ErrorOr<ErrorOr.Success> ApplyGravity(float gravity, float maxFallSpeed) => ErrorOr.Result.Success;
        public ErrorOr.ErrorOr<ErrorOr.Success> Stop() => ErrorOr.Result.Success;
    }

    private sealed class Boss1MockEffectPort : IEffectPort
    {
        public ErrorOr.ErrorOr<ErrorOr.Success> RequestTelegraph(float x, float y) => ErrorOr.Result.Success;
    }

    private sealed class Boss1MockAttackPort : IAttackPort
    {
        public ErrorOr.ErrorOr<ErrorOr.Success> CreateHitbox(BossSkill skill, float x, float y) => ErrorOr.Result.Success;
        public ErrorOr.ErrorOr<ErrorOr.Success> ClearBossBullets() => ErrorOr.Result.Success;
        public ErrorOr.ErrorOr<ErrorOr.Success> EmitBullet(BossSkill skill, float x, float y) => ErrorOr.Result.Success;
    }

    private sealed class Boss1MockRandomSource : IRandomSource
    {
        public int NextInt(int exclusiveMax) => 0;
        public float NextSingle() => 0.0f;
    }

    private sealed class Boss1MockConfigProvider : IConfigProvider
    {
        public BossConfigSnapshot Current => new(1, "Test");
    }

    private sealed class Boss1MockObservationService : IObservationService
    {
        public ErrorOr.ErrorOr<ErrorOr.Success> Record(in ObservationEvent observation) => ErrorOr.Result.Success;
    }

    /// <summary>
    /// DslBoss1TelegraphModule が予兆完了後に正しい Execute 状態へ遷移要求を発行することを検証します。
    /// </summary>
    [Fact]
    public void DslBoss1TelegraphModule_Lifecycle_TransitionsToExecute()
    {
        Boss1DefaultConfiguration config = new();
        Boss1BehaviorState state = new();
        state.CurrentSkill = BossSkill.Aimed3Shot;
        AnimalBattle.Ai.Dsl.Modules.DslBoss1TelegraphModule module = new(config, state);

        Boss1MockMovementPort movement = new();
        Boss1MockAttackPort attack = new();
        Boss1MockEffectPort effect = new();
        Boss1MockRandomSource random = new();
        Boss1MockConfigProvider configProvider = new();
        Boss1MockObservationService observation = new();

        StateContext context = new(
            entityId: 1,
            bossType: BossType.Boss1,
            configProvider: configProvider,
            configSnapshot: configProvider.Current,
            movement: movement,
            attack: attack,
            effect: effect,
            observation: observation,
            random: random);

        LifecycleInvocation invocation = new(BossType.Boss1, 1, BossState.Boss1_Telegraph, LifecycleOperation.Update, 1, 0.016f, Phase.Phase1);

        PlayerObservation obs1 = new(10, true, 0, 0, 0, 0, 1);
        context.BeginFrame(1, 0.016f, Phase.Phase1, in obs1);
        module.Enter(ref context, in invocation);

        // Aimed3Shot の予兆時間（0.40s）を経過させる
        PlayerObservation obs2 = new(10, true, 0, 0, 0, 0, 2);
        context.BeginFrame(2, 0.45f, Phase.Phase1, in obs2);
        module.Update(ref context, in invocation);

        Assert.Equal(BossState.Boss1_Execute, state.PendingBossState);
    }

    /// <summary>
    /// DslBoss1ExecuteModule が突進（DiveRam）完了後に正しい Recovery 状態へ遷移要求を発行することを検証します。
    /// </summary>
    [Fact]
    public void DslBoss1ExecuteModule_DiveRam_TransitionsToRecovery()
    {
        Boss1DefaultConfiguration config = new();
        Boss1BehaviorState state = new();
        state.CurrentSkill = BossSkill.DiveRam;
        AnimalBattle.Ai.Dsl.Modules.DslBoss1ExecuteModule module = new(config, state);

        Boss1MockMovementPort movement = new();
        Boss1MockAttackPort attack = new();
        Boss1MockEffectPort effect = new();
        Boss1MockRandomSource random = new();
        Boss1MockConfigProvider configProvider = new();
        Boss1MockObservationService observation = new();

        StateContext context = new(
            entityId: 1,
            bossType: BossType.Boss1,
            configProvider: configProvider,
            configSnapshot: configProvider.Current,
            movement: movement,
            attack: attack,
            effect: effect,
            observation: observation,
            random: random);

        LifecycleInvocation invocation = new(BossType.Boss1, 1, BossState.Boss1_Execute, LifecycleOperation.Update, 1, 0.016f, Phase.Phase1);

        PlayerObservation obs1 = new(10, true, 100, 100, 0, 0, 1);
        context.BeginFrame(1, 0.016f, Phase.Phase1, in obs1);
        module.Enter(ref context, in invocation);

        // DiveRam の持続時間（0.45s）を経過させる
        PlayerObservation obs2 = new(10, true, 100, 100, 0, 0, 2);
        context.BeginFrame(2, 0.50f, Phase.Phase1, in obs2);
        module.Update(ref context, in invocation);

        Assert.Equal(BossState.Boss1_Recovery, state.PendingBossState);
    }

    /// <summary>
    /// DslBoss1RecoveryModule が後隙完了後に正しい Hover 状態へ遷移要求を発行することを検証します。
    /// </summary>
    [Fact]
    public void DslBoss1RecoveryModule_Lifecycle_TransitionsToHover()
    {
        Boss1DefaultConfiguration config = new();
        Boss1BehaviorState state = new();
        state.CurrentSkill = BossSkill.DiveRam;
        AnimalBattle.Ai.Dsl.Modules.DslBoss1RecoveryModule module = new(config, state);

        Boss1MockMovementPort movement = new();
        Boss1MockAttackPort attack = new();
        Boss1MockEffectPort effect = new();
        Boss1MockRandomSource random = new();
        Boss1MockConfigProvider configProvider = new();
        Boss1MockObservationService observation = new();

        StateContext context = new(
            entityId: 1,
            bossType: BossType.Boss1,
            configProvider: configProvider,
            configSnapshot: configProvider.Current,
            movement: movement,
            attack: attack,
            effect: effect,
            observation: observation,
            random: random);

        LifecycleInvocation invocation = new(BossType.Boss1, 1, BossState.Boss1_Recovery, LifecycleOperation.Update, 1, 0.016f, Phase.Phase1);

        PlayerObservation obs1 = new(10, true, 0, 0, 0, 0, 1);
        context.BeginFrame(1, 0.016f, Phase.Phase1, in obs1);
        module.Enter(ref context, in invocation);

        // DiveRam の後隙時間（1.40s）を経過させる
        PlayerObservation obs2 = new(10, true, 0, 0, 0, 0, 2);
        context.BeginFrame(2, 1.45f, Phase.Phase1, in obs2);
        module.Update(ref context, in invocation);

        Assert.Equal(BossState.Boss1_Hover, state.PendingBossState);
    }

    /// <summary>
    /// DslBoss1ExecuteModule が弾幕攻撃完了後に正しい Recovery 状態へ遷移要求を発行することを検証します。
    /// </summary>
    [Fact]
    public void DslBoss1ExecuteModule_Barrage_TransitionsToRecovery()
    {
        Boss1DefaultConfiguration config = new();
        Boss1BehaviorState state = new();
        state.CurrentSkill = BossSkill.Aimed3Shot;
        AnimalBattle.Ai.Dsl.Modules.DslBoss1ExecuteModule module = new(config, state);

        Boss1MockMovementPort movement = new();
        Boss1MockAttackPort attack = new();
        Boss1MockEffectPort effect = new();
        Boss1MockRandomSource random = new();
        Boss1MockConfigProvider configProvider = new();
        Boss1MockObservationService observation = new();

        StateContext context = new(
            entityId: 1,
            bossType: BossType.Boss1,
            configProvider: configProvider,
            configSnapshot: configProvider.Current,
            movement: movement,
            attack: attack,
            effect: effect,
            observation: observation,
            random: random);

        LifecycleInvocation invocation = new(BossType.Boss1, 1, BossState.Boss1_Execute, LifecycleOperation.Update, 1, 0.016f, Phase.Phase1);

        PlayerObservation obs1 = new(10, true, 200, 200, 0, 0, 1);
        context.BeginFrame(1, 0.016f, Phase.Phase1, in obs1);
        module.Enter(ref context, in invocation);

        // Aimed3Shot の持続時間（0.90s）を経過させる
        PlayerObservation obs2 = new(10, true, 200, 200, 0, 0, 2);
        context.BeginFrame(2, 0.95f, Phase.Phase1, in obs2);
        module.Update(ref context, in invocation);

        Assert.Equal(BossState.Boss1_Recovery, state.PendingBossState);
    }
}


