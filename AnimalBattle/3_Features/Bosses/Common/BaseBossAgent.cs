using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Ai;
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

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public virtual float HoverTimer => 0.0f;

    /// <summary>Boss 描画時の回転角（ラジアン）を取得します。</summary>
    public virtual double Rotation => 0.0;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public virtual float TargetX => Brain.TargetX;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public virtual float TargetY => Brain.TargetY;

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    public virtual bool IsVulnerable => AbilitySystem.HasTag(GameplayTag.Vulnerable);

    /// <summary>Boss がスーパーアーマー（剛体）状態であるかを取得します。</summary>
    public virtual bool HasSuperArmor => AbilitySystem.HasTag(GameplayTag.SuperArmor);

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => Brain.FacingRight;

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
        float spawnX,
        float spawnY,
        float scaleX,
        float scaleY)
    {
        entity.Set(new Transform(spawnX, spawnY, scaleX, scaleY))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)));

        Brain.AbilitySystem.SetOwner(entity);
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
        SetVelocity(0.0f, 0.0f);
        FlipX = false;
        IsVisible = true;
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