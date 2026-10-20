using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Ai;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェント共通の基底抽象クラス。
///     Pawn を継承し、AI 意思決定ブレインと ECS Entity 間の同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossAgent : Pawn, IDisposable {
    /// <summary>弾幕角度変調用オフセット。</summary>
    protected float EmitterAngleOffset;

    /// <summary>
    ///     対象の Flecs エンティティを指定して基底クラスを初期化します。
    /// </summary>
    /// <param name="entity">Flecs エンティティ句柄。</param>
    protected BaseBossAgent(Entity entity) : base(entity)
    {
    }

    /// <summary>現在の Flecs ワールドを取得します。</summary>
    public World CurrentWorld { get; private set; }

    /// <summary>Boss 意思決定ブレインの基底参照を取得します。</summary>
    public abstract IBossBrain Brain { get; }

    /// <summary>Boss のアビリティシステムコンポーネントを取得します。</summary>
    public virtual AbilitySystemComponent AbilitySystem => Brain.AbilitySystem;

    /// <summary>Boss の種別を取得します。</summary>
    public abstract BossType BossType { get; }

    /// <summary>現在の戦闘 Phase を取得します。</summary>
    public virtual Phase Phase => Brain.Phase;

    /// <summary>設定ファイル駆動の Phase 2 / 3 移行体力閾値比率（0.0 ~ 1.0）を取得します。</summary>
    public abstract (float Phase2Threshold, float Phase3Threshold) PhaseThresholds { get; }

    /// <summary>現在の Boss 状態を取得します。</summary>
    public virtual BossState CurrentState => Brain.CurrentState;

    /// <summary>現在選択されている攻撃技能識別タグを取得します。</summary>
    public virtual StringId CurrentSkillTag => Brain.CurrentSkillTag;

    /// <summary>現在 AI 状態の経過時間（秒）を取得します。</summary>
    public virtual float StateTimer => Brain.StateTimer;

    /// <summary>AI 目標地点座標を取得します。</summary>
    public virtual Vec2f TargetPosition => Brain.TargetPosition;

    /// <summary>Boss が後隙による易傷状態であるかを取得します（Entity の GameplayTagState を単一真理源とする）。</summary>
    public virtual bool IsVulnerable => Entity.Get<GameplayTagState>().HasTag(GameplayTag.Vulnerable);

    /// <summary>Boss がスーパーアーマー（剛体）状態であるかを取得します（Entity の GameplayTagState を単一真理源とする）。</summary>
    public virtual bool HasSuperArmor => Entity.Get<GameplayTagState>().HasTag(GameplayTag.SuperArmor);

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => Brain.FacingRight;

    /// <summary>Boss が死亡状態であるかを取得します。</summary>
    public virtual bool IsDead => Brain.IsDead || (Entity.IsAlive() && Entity.Has<DeadTag>()) || Health.Current <= 0.0f;

    /// <summary>
    ///     Boss エージェントのリソースを解放します。
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     ブレインの波次発射および弾幕消去イベントをエージェントにバインドします。
    /// </summary>
    protected void BindBrainEvents(IBossBrain brain)
    {
        brain.OnEmitBulletWave += HandleEmitBulletWave;
        brain.OnClearBullets += HandleClearBullets;
    }

    /// <summary>
    ///     実行時スプライト画像および空間変換を Entity に登録し、Brain のアビリティシステム所有者をバインドします。
    ///     （基礎パラメータ、判定形状および陣営タグは Prefab 原型より自動継承されます）
    /// </summary>
    protected void InitializeComponents(
        Entity entity,
        int spriteHandle,
        Vec2f spawn,
        Vec2f scale)
    {
        entity.Set(new Transform(spawn, scale))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)));

        Brain.AbilitySystem.OnTagsChanged += tags =>
        {
            if (entity.IsAlive() && entity.Has<GameplayTagState>())
            {
                entity.GetMut<GameplayTagState>().ActiveMask = tags;
            }
        };
        if (entity.Has<GameplayTagState>())
        {
            entity.GetMut<GameplayTagState>().ActiveMask = Brain.AbilitySystem.Tags.RawTags;
        }
    }

    /// <summary>
    ///     現在のフレーム ワールド参照を設定します。
    /// </summary>
    protected void SetCurrentWorld(World world)
    {
        CurrentWorld = world;
    }

    /// <summary>
    ///     体力を検査し、死亡判定時にブレインを終了させます。
    /// </summary>
    /// <returns>死亡している場合は true。</returns>
    protected bool CheckAndHandleDeath()
    {
        ref var health = ref Entity.GetMut<Health>();
        if (health.Current <= 0.0f)
        {
            if (!Brain.IsDead) Brain.Kill();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Entity の BossCombatState コンポーネントを最新状態に同期します。
    /// </summary>
    protected void SyncCombatState()
    {
        Entity.Set(new BossCombatState(Phase));
    }

    /// <summary>
    ///     デバッグ用に指定フェーズへ即座に切り替え、体力・弾幕および状態を同期します。
    /// </summary>
    public void DebugSetPhase(Phase targetPhase)
    {
        if (!IsAlive || IsDead || Health.Current <= 0.0f || (Entity.IsAlive() && Entity.Has<DeadTag>())) return;

        Brain.DebugSetPhase(targetPhase);
        var thresholds = PhaseThresholds;
        ref var hp = ref Entity.GetMut<Health>();
        var ratio = targetPhase switch {
            Phase.Phase1 => 1.0f,
            Phase.Phase2 => thresholds.Phase2Threshold,
            Phase.Phase3 => thresholds.Phase3Threshold,
            _ => 1.0f
        };
        hp.Current = Math.Clamp((float)Math.Round(hp.Max * ratio), 1.0f, hp.Max);
        HandleClearBullets();
        SyncCombatState();
    }

    /// <summary>
    ///     弾幕波次発射イベントを処理します。
    /// </summary>
    protected abstract void HandleEmitBulletWave(StringId skill, Vec2f origin, Vec2f target);

    /// <summary>
    ///     フィールド上に存在するすべての Boss 弾幕を一括消去します。
    /// </summary>
    protected void HandleClearBullets()
    {
        if (CurrentWorld != default) BulletSpawner.ClearAllBossBullets(CurrentWorld);
    }

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・判定更新を実行します。
    /// </summary>
    public abstract void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery);


    /// <summary>
    ///     Boss の状態を初期位置および初期パラメータへリセットします。
    /// </summary>
    public virtual void Reset()
    {
        EmitterAngleOffset = 0.0f;
        OnReset();
        SetVelocity(Vec2f.Zero);
        SetFlipX(false);
        SetVisible(true);
        ClearTransientEffects();
        if (Entity.IsAlive())
        {
            Entity.Set(new TimedEffectsState());
        }
        Entity.Set(new BossCombatState(Phase));
    }

    /// <summary>フレーム末尾におけるサブクラス固有の演出・描画同期を実行します。</summary>
    protected abstract void OnPostUpdate(float deltaTime, World world);

    /// <summary>リセット時のサブクラス固有の座標・体力・状態復元を実行します。</summary>
    protected abstract void OnReset();
}