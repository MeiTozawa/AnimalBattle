using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Components;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェント共通の基底抽象クラス。
///     Pawn を継承し、AI 意思決定ブレインと ECS Entity 間の同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossAgent : Pawn, IDisposable
{
    private World _currentWorld;

    /// <summary>弾幕角度変調用オフセット。</summary>
    protected float EmitterAngleOffset;

    /// <summary>現在の Flecs ワールドを取得します。</summary>
    public World CurrentWorld => _currentWorld;

    /// <summary>Boss 意思決定ブレインの基底参照を取得します。</summary>
    public abstract IBossBrain Brain { get; }

    /// <summary>Boss の種別を取得します。</summary>
    public abstract BossType BossType { get; }

    /// <summary>現在の戦闘 Phase を取得します。</summary>
    public virtual Phase Phase => Brain.Phase;

    /// <summary>現在の Boss 状態を取得します。</summary>
    public virtual BossState CurrentState => Brain.CurrentState;

    /// <summary>現在選択されている攻撃技能種別を取得します。</summary>
    public virtual BossSkill CurrentSkill => Brain.CurrentSkill;

    /// <summary>現在状態の経過時間を取得します。</summary>
    public virtual float StateTimer => Brain.StateTimer;

    /// <summary>現在状態の目標持続時間を取得します。</summary>
    public virtual float StateDuration => Brain.StateDuration;

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public virtual float HoverTimer => 0.0f;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public virtual float TargetX => Brain.TargetX;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public virtual float TargetY => Brain.TargetY;

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    public virtual bool IsVulnerable => Brain.IsVulnerable;

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => Brain.FacingRight;

    /// <summary>
    ///     対象の Flecs エンティティを指定して基底クラスを初期化します。
    /// </summary>
    /// <param name="entity">Flecs エンティティ句柄。</param>
    protected BaseBossAgent(Entity entity) : base(entity)
    {
    }

    /// <summary>
    ///     ブレインの波次発射および弾幕消去イベントをエージェントにバインドします。
    /// </summary>
    protected void BindBrainEvents(IBossBrain brain)
    {
        brain.OnEmitBarrageWave += HandleEmitBarrageWave;
        brain.OnClearBullets += HandleClearBullets;
    }

    /// <summary>
    ///     共通コンポーネント群を Entity に一括登録します。
    /// </summary>
    protected void InitializeCommonComponents(
        Entity entity,
        int spriteHandle,
        float maxHp,
        float spawnX,
        float spawnY,
        float scaleX,
        float scaleY,
        CollisionShape hurtboxShape,
        CollisionShape hitboxShape,
        float bodyContactDamage,
        float collisionWidth = 0.0f,
        float collisionHeight = 0.0f)
    {
        entity.Set(new Health(maxHp))
            .Set(new InvincibleOnHit(0.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new Transform(spawnX, spawnY, scaleX, scaleY))
            .Set(new Velocity(0.0f))
            .Set(new Hurtbox(hurtboxShape, Faction.Hostile))
            .Set(new Hitbox(hitboxShape, bodyContactDamage, Faction.Hostile, false))
            .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new BossCombatState(IsVulnerable, Phase))
            .Add<BossTag>();

        if (collisionWidth > 0.0f && collisionHeight > 0.0f)
        {
            entity.Set(new CollisionBox(collisionWidth, collisionHeight));
        }
    }

    /// <summary>
    ///     現在のフレーム ワールド参照を設定します。
    /// </summary>
    protected void SetCurrentWorld(World world)
    {
        _currentWorld = world;
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
        Entity.Set(new BossCombatState(IsVulnerable, Phase));
    }

    /// <summary>
    ///     弾幕波次発射イベントを処理します。
    /// </summary>
    protected abstract void HandleEmitBarrageWave(BossSkill skill, Vec2f origin, Vec2f target);

    /// <summary>
    ///     フィールド上に存在するすべての Boss 弾幕を一括消去します。
    /// </summary>
    protected void HandleClearBullets()
    {
        if (_currentWorld != default)
        {
            CombatEffectHelper.ClearAllBossBullets(_currentWorld);
        }
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
        try
        {
            EmitterAngleOffset = 0.0f;
            OnReset();
            SetVelocity(0.0f, 0.0f);
            FlipX = false;
            IsVisible = true;
            ClearTransientEffects();
            Entity.Set(new BossCombatState(IsVulnerable, Phase));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BaseBossAgent 警告] Reset 処理中に例外が発生しました: {ex.Message}");
        }
    }

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
