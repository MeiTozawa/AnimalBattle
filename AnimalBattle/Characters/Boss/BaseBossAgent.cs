using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     多態 Boss 自治エージェント共通の基底抽象クラス。
///     <see cref="Pawn" /> を継承し、AI 意思決定ブレインと ECS Entity 間の同期ライフサイクルを一元管理します。
/// </summary>
public abstract class BaseBossAgent : Pawn, IDisposable
{
    /// <summary>Boss の種別を取得します。</summary>
    public abstract BossType BossType { get; }

    /// <summary>現在の戦闘 Phase を取得します。</summary>
    public abstract Phase Phase { get; }

    /// <summary>現在の Boss 状態を取得します。</summary>
    public abstract BossState CurrentState { get; }

    /// <summary>現在選択されている攻撃技能種別を取得します。</summary>
    public abstract BossSkill CurrentSkill { get; }

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public abstract float StateTimer { get; }

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public abstract float StateDuration { get; }

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public virtual float HoverTimer => 0.0f;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public virtual float TargetX => X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public virtual float TargetY => Y;

    /// <summary>Boss が後隙による易傷状態であるかを取得します。</summary>
    public abstract bool IsVulnerable { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public virtual bool FacingRight => true;

    /// <summary>
    ///     対象の Flecs エンティティを指定して基底クラスを初期化します。
    /// </summary>
    /// <param name="entity">Flecs エンティティ句柄。</param>
    protected BaseBossAgent(Entity entity) : base(entity)
    {
    }

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・判定更新を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    /// <param name="playerPosition">プレイヤー自機の現在座標。</param>
    /// <param name="playerVelocity">プレイヤー自機の現在速度。</param>
    /// <param name="world">Flecs ECS ワールド。</param>
    /// <param name="platformQuery">足場・地形判定用クエリ。</param>
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
            OnReset();
            SetVelocity(0.0f, 0.0f);
            FlipX = false;
            IsVisible = true;
            ClearTransientEffects();
            Entity.Set(new Components.BossCombatState(IsVulnerable, Phase));
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
