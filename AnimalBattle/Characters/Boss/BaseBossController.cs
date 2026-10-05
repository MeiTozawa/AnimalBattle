using System;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss コントローラー共通の基底抽象クラス。
///     AI 状態機械の更新、コマンド実行、および戦闘状態同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossController : IBossController, ICharacter
{
    private int _aiErrorLoggedCount;

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity { get; }

    /// <summary>Boss の種別を取得します。</summary>
    public abstract BossType BossType { get; }

    /// <summary>Boss AI ランタイムを取得します。</summary>
    protected BossAiRuntime Runtime { get; }

    /// <summary>Boss 実行時データスロットを取得します。</summary>
    protected BossRuntimeSlot Slot { get; }

    /// <summary>観測サービスを取得します。</summary>
    protected ObservationService Observation { get; }

    /// <summary>現在の実行フレーム数を取得または設定します。</summary>
    protected long FrameNumber { get; set; }

    /// <summary>Boss のヘルス情報を取得します。</summary>
    public ref readonly Health Health => ref Entity.Get<Health>();

    /// <summary>現在の Phase を取得します。</summary>
    public Phase Phase => Slot.Phase;

    /// <summary>現在の Boss 状態を取得します。</summary>
    public BossState CurrentState => Slot.CurrentStateId;

    /// <summary>現在選択されている攻撃技能種別を取得します。</summary>
    public abstract BossSkill CurrentSkill { get; }

    /// <summary>現在選択されている攻撃技能名を取得します。</summary>
    public string SkillName => CurrentSkill != BossSkill.None ? CurrentSkill.ToString() : string.Empty;

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public abstract float StateTimer { get; }

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration => GetCurrentStateDuration();

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public abstract float HoverTimer { get; }

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => Slot.TargetPositionX;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => Slot.TargetPositionY;

    /// <summary>Boss が後隙による易傷（被ダメージ倍加）状態であるかを取得します。</summary>
    public abstract bool IsVulnerable { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => Slot.MovementData.FacingRight;

    /// <summary>接地状態であるかを取得します。</summary>
    protected virtual bool IsGrounded => false;

    /// <summary>被ダメージ倍率を取得します（易傷状態時は倍加）。</summary>
    public float DamageMultiplier => IsVulnerable ? ConfigManager.Player.VulnerableDamageMultiplier : 1.0f;

    /// <summary>ワールド X 座標を取得または設定します。</summary>
    public float X
    {
        get => Entity.Get<Transform>().X;
        set => Entity.GetMut<Transform>().X = value;
    }

    /// <summary>ワールド Y 座標を取得または設定します。</summary>
    public float Y
    {
        get => Entity.Get<Transform>().Y;
        set => Entity.GetMut<Transform>().Y = value;
    }

    /// <summary>X 軸移動速度を取得または設定します。</summary>
    public float Vx
    {
        get => Entity.Get<Velocity>().Vx;
        set => Entity.GetMut<Velocity>().Vx = value;
    }

    /// <summary>Y 軸移動速度を取得または設定します。</summary>
    public float Vy
    {
        get => Entity.Get<Velocity>().Vy;
        set => Entity.GetMut<Velocity>().Vy = value;
    }

    /// <summary>
    ///     基底 Boss コントローラーを初期化します。
    /// </summary>
    /// <param name="entity">Flecs エンティティ句柄。</param>
    /// <param name="runtime">Boss AI ランタイム。</param>
    /// <param name="slot">Boss 実行時スロット。</param>
    /// <param name="observation">観測サービス。</param>
    protected BaseBossController(
        Entity entity,
        BossAiRuntime runtime,
        BossRuntimeSlot slot,
        ObservationService observation)
    {
        Entity = entity;
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
        if (updateResult.IsError && _aiErrorLoggedCount++ < 3)
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

        ref var v = ref Entity.GetMut<Velocity>();
        v.Vx = 0.0f;
        v.Vy = 0.0f;

        ref var spr = ref Entity.GetMut<Sprite>();
        spr.IsVisible = true;
        spr.FlipX = false;

        if (Entity.IsAlive()) Entity.Remove<HitFlashEffect>();

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
        // マネージドリソースの追加解放処理なし
    }
}
