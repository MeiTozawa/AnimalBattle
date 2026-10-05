using System;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェント共通の基底抽象クラス。
///     <see cref="Pawn" /> を継承し、AI 状態機械の更新、コマンド適用、および戦闘状態同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossAgent : Pawn, IBossAgent
{
    /// <inheritdoc />
    public BossType BossType => Slot.AiData.BossType;

    /// <summary>Boss AI ランタイムを取得します。</summary>
    protected BossAiRuntime Runtime { get; }

    /// <summary>Boss 実行時データスロットを取得します。</summary>
    protected BossRuntimeSlot Slot { get; }

    /// <summary>観測サービスを取得します。</summary>
    protected ObservationService Observation { get; }

    /// <summary>現在の実行フレーム数を取得または設定します。</summary>
    protected long FrameNumber { get; set; }

    /// <inheritdoc />
    public Phase Phase => Slot.Phase;

    /// <inheritdoc />
    public BossState CurrentState => Slot.CurrentStateId;

    /// <inheritdoc />
    public abstract BossSkill CurrentSkill { get; }

    /// <inheritdoc />
    public string SkillName => CurrentSkill != BossSkill.None ? CurrentSkill.ToString() : string.Empty;

    /// <inheritdoc />
    public abstract float StateTimer { get; }

    /// <inheritdoc />
    public float StateDuration => GetCurrentStateDuration();

    /// <inheritdoc />
    public virtual float HoverTimer => 0.0f;

    /// <inheritdoc />
    public float TargetX => Slot.TargetPositionX;

    /// <inheritdoc />
    public float TargetY => Slot.TargetPositionY;

    /// <inheritdoc />
    public abstract bool IsVulnerable { get; }

    /// <inheritdoc />
    public virtual bool FacingRight => Slot.MovementData.FacingRight;

    /// <summary>被ダメージ倍率を取得します（易傷状態時は倍加）。</summary>
    public float DamageMultiplier => IsVulnerable ? ConfigManager.Player.VulnerableDamageMultiplier : 1.0f;

    /// <summary>
    ///     対象の Flecs エンティティと AI 定義を指定して基底クラスを初期化します。
    /// </summary>
    /// <param name="entity">Flecs エンティティ句柄。</param>
    /// <param name="runtime">Boss AI ランタイム。</param>
    /// <param name="slot">Boss 実行時スロット。</param>
    /// <param name="observation">観測サービス。</param>
    protected BaseBossAgent(
        Entity entity,
        BossAiRuntime runtime,
        BossRuntimeSlot slot,
        ObservationService observation) : base(entity)
    {
        Runtime = runtime;
        Slot = slot;
        Observation = observation;
    }

    /// <inheritdoc />
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        ref var health = ref Entity.GetMut<Health>();
        if (Slot.AiData.IsInvulnerable)
        {
            health.InvincibleTimer = MathF.Max(health.InvincibleTimer, 0.1f);
        }

        if (health.Current <= 0.0f) return;

        PlayerObservation playerObservation = new(
            1,
            true,
            playerPosition.X,
            playerPosition.Y,
            playerVelocity.X,
            playerVelocity.Y,
            FrameNumber);

        Runtime.SynchronizeBossState(BossType, X, Y, Vx, Vy, IsGrounded, playerObservation.IsAlive);
        var updateResult =
            Runtime.Update(Slot, FrameNumber++, deltaTime, Phase, in playerObservation, health.Current, health.Max);
        if (updateResult.IsError)
            Console.WriteLine(
                $"[{GetType().Name}] AI 更新エラー (Frame {FrameNumber - 1}): {updateResult.FirstError.Description}");

        BossCommandExecutor.ExecuteMovement(world, Entity, Slot, deltaTime, platformQuery);
        BossCommandExecutor.ExecuteAttack(world, Entity, Slot);
        BossCommandExecutor.ExecuteEffects(world, Entity, Slot);

        OnPostUpdate(deltaTime, world);

        Entity.Set(new BossCombatState(IsVulnerable, Phase));
        Slot.AiData.StateTimer = StateTimer;
        Slot.AiData.StateDuration = StateDuration;
    }

    /// <summary>
    ///     プレイヤー移動速度を省略した互換用更新メソッド。
    /// </summary>
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        Update(deltaTime, playerPosition, Vec2f.Zero, world, platformQuery);
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        FrameNumber = 0;

        OnReset();

        SetVelocity(0.0f, 0.0f);
        FlipX = false;
        IsVisible = true;
        ClearTransientEffects();

        Entity.Set(new BossCombatState(IsVulnerable, Phase));
        Slot.AiData.StateTimer = StateTimer;
        Slot.AiData.StateDuration = StateDuration;
    }

    /// <summary>サブクラス固有の状態目標持続時間（秒）を算出します。</summary>
    protected abstract float GetCurrentStateDuration();

    /// <summary>フレーム末尾におけるサブクラス固有の演出・描画同期を実行します。</summary>
    protected abstract void OnPostUpdate(float deltaTime, World world);

    /// <summary>リセット時のサブクラス固有の座標・体力・状態復元を実行します。</summary>
    protected abstract void OnReset();

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
