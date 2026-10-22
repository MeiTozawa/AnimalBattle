using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Asset;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     近接斬撃攻撃の判定ボリューム（Hitbox）および付随する視覚エフェクトスプライトの生成・制御を担当する静的サービスです。
///     自機近接斬撃判定は常駐エンティティの .Enable() / .Disable() により制御され、ゼロアロケーションで実行されます。
/// </summary>
public static class MeleeSpawner
{
    /// <summary>
    ///     敵性近接斬撃判定（矩形 Hitbox ＋ 斬撃スプライト視覚エフェクト）を生成します。
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
        var assets = world.Get<AssetManager>();
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
    ///     自機常駐の近接攻撃判定（矩形 Hitbox ＋ 斬撃スプライト視覚エフェクト）を設定し、有効化（.Enable()）します。
    ///     頻繁な生成・破棄を行わず常駐エンティティの有効/無効切り替えによってゼロアロケーションを実現します。
    /// </summary>
    public static void ActivatePlayerMeleeAttack(
        Entity meleeEntity,
        Vec2f pos,
        Vec2f size,
        float damage,
        SlashDirection direction)
    {
        var assets = meleeEntity.CsWorld().Get<AssetManager>();
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
}
