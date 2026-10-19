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
        float x,
        float y,
        float vx,
        float vy,
        float width,
        float height,
        float damage,
        float lifetime,
        Color color,
        SlashDirection direction = SlashDirection.Right)
    {
        var hasTexture =
            AssetManager.TryGetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng, out var slashHandle);
        Debug.Assert(hasTexture, $"近接斬撃エフェクトテクスチャがロードされていません: {ResourcePath.Characters.SwordShieldEffectSpritesPng}");

        var slashCfg = world.Get<RenderConfig>().SlashEffect;
        var colIndex = slashCfg.SpriteColIndex;
        var rowIndex = (int)direction;
        var cellSize = slashCfg.SpriteCellSize;
        var srcRect = Rect.FromWidthHeight(colIndex * cellSize, rowIndex * cellSize, cellSize, cellSize);
        var scale = MathF.Max(width, height) / slashCfg.BossBaseReferenceSize;

        // セル内の斬撃ピクセル重心偏りを逆補正して視覚中心を (x, y) に一致させる
        var offsetX = direction switch {
            SlashDirection.Right => slashCfg.CenterBiasOffset * scale,
            SlashDirection.Left => -slashCfg.CenterBiasOffset * scale,
            _ => 0.0f
        };
        var offsetY = direction switch {
            SlashDirection.Down => slashCfg.CenterBiasOffset * scale,
            SlashDirection.Up => -slashCfg.CenterBiasOffset * scale,
            _ => 0.0f
        };

        CombatPoolService.RentAreaEffect()
           .Set(new Transform(x + offsetX, y + offsetY, scale, scale))
           .Set(new Velocity(vx, vy))
           .Set(new Hitbox(new CollisionShape.Box(width, height, -offsetX, -offsetY), damage, Faction.Hostile, false))
           .Set(new Sprite(slashHandle, false, true, srcRect, color))
           .Set(new FadeEffect(lifetime));
    }

    /// <summary>
    ///     自機常駐の近接攻撃判定（矩形 Hitbox ＋ 斬撃スプライト視覚エフェクト）を設定し、有効化（.Enable()）します。
    ///     頻繁な生成・破棄を行わず常駐エンティティの有効/無効切り替えによってゼロアロケーションを実現します。
    /// </summary>
    public static void ActivatePlayerMeleeAttack(
        Entity meleeEntity,
        float x,
        float y,
        float width,
        float height,
        float damage,
        SlashDirection direction)
    {
        var hasTexture =
            AssetManager.TryGetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng, out var slashHandle);
        Debug.Assert(hasTexture, $"近接斬撃エフェクトテクスチャがロードされていません: {ResourcePath.Characters.SwordShieldEffectSpritesPng}");

        var slashCfg = meleeEntity.CsWorld().Get<RenderConfig>().SlashEffect;
        var colIndex = slashCfg.SpriteColIndex;
        var rowIndex = (int)direction;
        var cellSize = slashCfg.SpriteCellSize;
        var srcRect = Rect.FromWidthHeight(colIndex * cellSize, rowIndex * cellSize, cellSize, cellSize);
        var scale = MathF.Max(width, height) / slashCfg.PlayerBaseReferenceSize;

        var offsetX = direction switch {
            SlashDirection.Right => slashCfg.CenterBiasOffset * scale,
            SlashDirection.Left => -slashCfg.CenterBiasOffset * scale,
            _ => 0.0f
        };
        var offsetY = direction switch {
            SlashDirection.Down => slashCfg.CenterBiasOffset * scale,
            SlashDirection.Up => -slashCfg.CenterBiasOffset * scale,
            _ => 0.0f
        };

        meleeEntity
           .Set(new Transform(x + offsetX, y + offsetY, scale, scale))
           .Set(new Velocity(0.0f, 0.0f))
           .Set(new Hitbox(new CollisionShape.Box(width, height, -offsetX, -offsetY), damage, Faction.Friendly, false))
           .Set(new HitHistory())
           .Set(new Sprite(slashHandle, false, true, srcRect, slashCfg.PlayerSlashColor))
           .Enable();
    }
}
