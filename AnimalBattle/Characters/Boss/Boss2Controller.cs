using AnimalBattle.Ai.Contracts;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 2（Ground Crusher）専用領域コントローラー。
///     地上巡回追跡、足場間自律跳躍、および壁激突気絶ライフサイクルを管理します。
/// </summary>
public sealed class Boss2Controller : IBossController
{
    private readonly BossCharacter _boss;

    /// <summary>
    ///     Boss 2 の Flecs エンティティ、テクスチャハンドル、およびナビゲーショングラフプロバイダーを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">足場尋路用ナビゲーショングラフプロバイダー（省略可能）。</param>
    public Boss2Controller(
        Entity entity,
        int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        System.Diagnostics.Debug.Assert(entity.IsAlive(), "Entity は有効でなければなりません。");
        _boss = new BossCharacter(entity, BossType.Boss2, spriteHandle, navGraphProvider);
    }

    /// <inheritdoc />
    public BossType BossType => _boss.BossType;

    /// <inheritdoc />
    public float CurrentHp => _boss.CurrentHp;

    /// <inheritdoc />
    public float MaxHp => _boss.MaxHp;

    /// <inheritdoc />
    public bool IsAlive => _boss.IsAlive;

    /// <inheritdoc />
    public Entity Entity => _boss.Entity;

    /// <inheritdoc />
    public Phase Phase => _boss.Phase;

    /// <inheritdoc />
    public BossState CurrentState => _boss.CurrentState;

    /// <inheritdoc />
    public BossSkill CurrentSkill => _boss.CurrentSkill;

    /// <inheritdoc />
    public string SkillName => _boss.SkillName;

    /// <inheritdoc />
    public float StateTimer => _boss.StateTimer;

    /// <inheritdoc />
    public float StateDuration => _boss.StateDuration;

    /// <inheritdoc />
    public float HoverTimer => _boss.HoverTimer;

    /// <inheritdoc />
    public bool FacingRight => _boss.FacingRight;

    /// <inheritdoc />
    public bool IsFlashing => _boss.IsFlashing;

    /// <inheritdoc />
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        _boss.Update(deltaTime, playerPosition, playerVelocity, world, platformQuery);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _boss.Reset();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // 現状マネージド参照のみのため追加解放処理なし
    }
}
