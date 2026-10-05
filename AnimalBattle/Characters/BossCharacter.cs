using System;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     多態 Boss コントローラー（Boss1Controller / Boss2Controller）への後方互換用 Facade ラッパー。
/// </summary>
[Obsolete("BossCharacter は廃止予定です。Boss1Controller または Boss2Controller を直接使用してください。")]
public sealed class BossCharacter : IBossController, ICharacter
{
    private readonly IBossController _controller;

    /// <summary>
    ///     Boss の Flecs エンティティ、種別およびスプライトハンドルを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="bossType">Boss 種別（Boss1 または Boss2）。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">Boss2 プラットフォーム尋路用ナビゲーショングラフプロバイダー（省略可能）。</param>
    public BossCharacter(
        Entity entity,
        BossType bossType,
        int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        _controller = bossType == BossType.Boss1
            ? new Boss1Controller(entity, spriteHandle)
            : new Boss2Controller(entity, spriteHandle, navGraphProvider);
    }

    /// <inheritdoc />
    public Entity Entity => _controller.Entity;

    /// <inheritdoc />
    public BossType BossType => _controller.BossType;

    /// <inheritdoc />
    public Phase Phase => _controller.Phase;

    /// <inheritdoc />
    public BossState CurrentState => _controller.CurrentState;

    /// <inheritdoc />
    public BossSkill CurrentSkill => _controller.CurrentSkill;

    /// <inheritdoc />
    public string SkillName => _controller.SkillName;

    /// <inheritdoc />
    public float StateTimer => _controller.StateTimer;

    /// <inheritdoc />
    public float StateDuration => _controller.StateDuration;

    /// <inheritdoc />
    public float HoverTimer => _controller.HoverTimer;

    /// <inheritdoc />
    public float TargetX => _controller.TargetX;

    /// <inheritdoc />
    public float TargetY => _controller.TargetY;

    /// <inheritdoc />
    public bool IsVulnerable => _controller.IsVulnerable;

    /// <inheritdoc />
    public bool FacingRight => _controller.FacingRight;

    /// <summary>Boss のヘルス情報を取得します。</summary>
    public ref readonly Health Health => ref Entity.Get<Health>();

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

    /// <summary>被ダメージ倍率を取得します（易傷状態時は倍加）。</summary>
    public float DamageMultiplier => IsVulnerable ? ConfigManager.Player.VulnerableDamageMultiplier : 1.0f;

    /// <inheritdoc />
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        _controller.Update(deltaTime, playerPosition, playerVelocity, world, platformQuery);
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
        _controller.Update(deltaTime, playerPosition, Vec2f.Zero, world, platformQuery);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _controller.Reset();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _controller.Dispose();
    }
}