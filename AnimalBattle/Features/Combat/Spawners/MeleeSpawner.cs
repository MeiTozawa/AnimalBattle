using System.Drawing;
using AnimalBattle.Components;

namespace AnimalBattle.Combat;

/// <summary>
///     近接斬撃攻撃の判定ボリューム：Hitboxおよび付随する視覚エフェクトスプライトの生成・制御を担当する静的サービスです。
///     自機近接斬撃判定は常駐エンティティの 有効化と無効化の切り替えにより制御され、ゼロアロケーションで実行されます。
/// </summary>
public static class MeleeSpawner
{
    /// <summary>
    ///     敵性近接斬撃判定：矩形 Hitbox ＋ 斬撃スプライト視覚エフェクトを生成します。
    /// </summary>
    public static void SpawnMeleeSlashHitbox(
        World world,
        Vec2f pos,
        Vec2f vel,
        Vec2f size,
        float damage,
        float lifetime,
        Color color,
        SlashDirection direction = SlashDirection.Right)
    {
        var assets = world.Get<IAssetManager>();
        var hasTexture =
            assets.TryGetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng, out var slashHandle);
        Debug.Assert(hasTexture, $"近接斬撃エフェクトテクスチャがロードされていません: {ResourcePath.Characters.SwordShieldEffectSpritesPng}");

        var slashCfg = world.Get<RenderConfig>().SlashEffect;
        var colIndex = slashCfg.SpriteColIndex;
        var rowIndex = (int)direction;
        var cellSize = slashCfg.SpriteCellSize;
        var srcRect = Rect.FromWidthHeight(colIndex * cellSize, rowIndex * cellSize, cellSize, cellSize);
        var scale = MathF.Max(size.X, size.Y) / slashCfg.BossBaseReferenceSize;

        // セル内の斬撃ピクセル重心偏りを逆補正して視覚中心を pos に一致させる
        var offset = direction switch {
            SlashDirection.Right => new Vec2f(slashCfg.CenterBiasOffset * scale, 0.0f),
            SlashDirection.Left => new Vec2f(-slashCfg.CenterBiasOffset * scale, 0.0f),
            SlashDirection.Down => new Vec2f(0.0f, slashCfg.CenterBiasOffset * scale),
            SlashDirection.Up => new Vec2f(0.0f, -slashCfg.CenterBiasOffset * scale),
            _ => Vec2f.Zero
        };

        CombatPoolService.RentBossSlashHitbox()
           .Set(new Transform(pos + offset, new Vec2f(scale, scale)))
           .Set(new Velocity(vel))
           .Set(new Hitbox(new CollisionShape.Box(size, -offset), damage, Faction.Hostile, false))
           .Set(new HitHistory())
           .Set(new Sprite(slashHandle, false, true, srcRect, color))
           .Set(new HitboxLifetime(lifetime));
    }

    /// <summary>
    ///     自機常駐の近接攻撃判定：矩形 Hitbox ＋ 斬撃スプライト視覚エフェクトを設定し、有効化します。
    ///     頻繁な生成・破棄を行わず常駐エンティティの有効/無効切り替えによってゼロアロケーションを実現します。
    /// </summary>
    public static void ActivatePlayerMeleeAttack(
        Entity meleeEntity,
        Vec2f pos,
        Vec2f size,
        float damage,
        SlashDirection direction)
    {
        var assets = meleeEntity.CsWorld().Get<IAssetManager>();
        var hasTexture =
            assets.TryGetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng, out var slashHandle);
        Debug.Assert(hasTexture, $"近接斬撃エフェクトテクスチャがロードされていません: {ResourcePath.Characters.SwordShieldEffectSpritesPng}");

        var slashCfg = meleeEntity.CsWorld().Get<RenderConfig>().SlashEffect;
        var colIndex = slashCfg.SpriteColIndex;
        var rowIndex = (int)direction;
        var cellSize = slashCfg.SpriteCellSize;
        var srcRect = Rect.FromWidthHeight(colIndex * cellSize, rowIndex * cellSize, cellSize, cellSize);
        var scale = MathF.Max(size.X, size.Y) / slashCfg.PlayerBaseReferenceSize;

        var offset = direction switch {
            SlashDirection.Right => new Vec2f(slashCfg.CenterBiasOffset * scale, 0.0f),
            SlashDirection.Left => new Vec2f(-slashCfg.CenterBiasOffset * scale, 0.0f),
            SlashDirection.Down => new Vec2f(0.0f, slashCfg.CenterBiasOffset * scale),
            SlashDirection.Up => new Vec2f(0.0f, -slashCfg.CenterBiasOffset * scale),
            _ => Vec2f.Zero
        };

        meleeEntity
           .Set(new Transform(pos + offset, new Vec2f(scale, scale)))
           .Set(new Velocity(Vec2f.Zero))
           .Set(new Hitbox(new CollisionShape.Box(size, -offset), damage, Faction.Friendly, false))
           .Set(new HitHistory())
           .Set(new Sprite(slashHandle, false, true, srcRect, slashCfg.PlayerSlashColor))
           .Enable();
    }

    /// <summary>
    ///     フィールド上に残存するすべての Boss 近接斬撃判定を一括消去します。
    ///     シミュレーション実行中のタイムライン競合および二重返却を防止するため、
    ///     即時返却ではなく <see cref="RecycleTag" /> を付与し、
    ///     フレーム末尾の <see cref="CombatRecycleSystem" /> へ回収を一元委譲します。
    /// </summary>
    public static void ClearAllBossMelee(World world)
    {
        using var query = world.QueryBuilder()
            .With<BossMeleeTag>()
            .Without<RecycleTag>()
            .Build();
        query.Each(static (Entity e) => e.Add<RecycleTag>());
    }

    /// <summary>
    ///     フィールド上に残存するすべての Boss 近接斬撃判定を即座にプールへ回収・返却します。
    ///     シーン破棄時：Teardownや戦闘リセット：ResetCombatなど、
    ///     確定的な同期リセット点で即時プール返却を実行します。
    /// </summary>
    public static void ClearAllBossMeleeImmediate(World world)
    {
        using var query = world.QueryBuilder()
            .With<BossMeleeTag>()
            .Build();
        query.Each(CombatPoolService.Recycle);
    }
}
