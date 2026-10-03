using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 1（Ancient Cat Overlord）専用領域コントローラー。
///     空中浮遊機動、全方位弾幕、および急降下攻撃ライフサイクルを管理します。
/// </summary>
public sealed class Boss1Controller : IBossController
{
    private readonly BossCharacter _boss;

    /// <summary>
    ///     Boss 1 の Flecs エンティティおよびテクスチャハンドルを指定してコントローラーを初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    public Boss1Controller(Entity entity, int spriteHandle)
    {
        System.Diagnostics.Debug.Assert(entity.IsAlive(), "Entity は有効でなければなりません。");
        _boss = new BossCharacter(entity, BossType.Boss1, spriteHandle);
    }

    /// <inheritdoc />
    public BossType BossType => _boss.BossType;


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
    public float TargetX => _boss.TargetX;

    /// <inheritdoc />
    public float TargetY => _boss.TargetY;

    /// <inheritdoc />
    public bool IsVulnerable => _boss.IsVulnerable;

    /// <inheritdoc />
    public bool FacingRight => _boss.FacingRight;


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
