using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェント共通の基底抽象クラス。
///     <see cref="Pawn" /> を継承し、AI 状態機械の更新、コマンド適用、および戦闘状態同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossAgent : Pawn, IDisposable
{
    /// <summary>Boss の種別を取得します。</summary>
    public BossType BossType => Slot.AiData.BossType;

    /// <summary>Boss AI ランタイムを取得します。</summary>
    protected BossAiRuntime Runtime { get; }

    /// <summary>Boss 実行時データスロットを取得します。</summary>
    protected BossRuntimeSlot Slot { get; }

    /// <summary>観測サービスを取得します。</summary>
    protected ObservationService Observation { get; }

    /// <summary>現在の実行フレーム数を取得または設定します。</summary>
    protected long FrameNumber { get; set; }

    /// <summary>現在の Phase を取得します。</summary>
    public Phase Phase => Slot.Phase;

    /// <summary>現在の Boss 状態を取得します。</summary>
    public BossState CurrentState => Slot.CurrentStateId;

    /// <summary>現在選択されている攻撃技能種別を取得します。</summary>
    public abstract BossSkill CurrentSkill { get; }

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public abstract float StateTimer { get; }

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration => GetCurrentStateDuration();

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public virtual float HoverTimer => 0.0f;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => Slot.TargetPositionX;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => Slot.TargetPositionY;

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    public abstract bool IsVulnerable { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => Slot.MovementData.FacingRight;

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

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・演出コマンド適用を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    /// <param name="playerPosition">プレイヤー自機の現在座標。</param>
    /// <param name="playerVelocity">プレイヤー自機の現在速度。</param>
    /// <param name="world">Flecs ECS ワールド。</param>
    /// <param name="platformQuery">足場・地形判定用クエリ。</param>
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

    /// <summary>
    ///     Boss の状態を初期位置および初期パラメータへリセットします。
    /// </summary>
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

    /// <summary>
    ///     Boss エージェントのリソースを解放します。
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
