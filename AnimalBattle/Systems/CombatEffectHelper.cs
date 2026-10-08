using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     戦闘演出（被弾スパーク・ジャンプエフェクト・衝撃波・撃破爆発等）および弾幕生成の補助メソッド群を提供する静的ヘルパークラス。
///     生成する敵弾に <see cref="Hitbox" />（敵性陣営・円形判定）を付与します。
/// </summary>
public static class CombatEffectHelper {
    /// <summary>
    ///     指定座標に Boss 弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">出現 X 座標</param>
    /// <param name="y">出現 Y 座標</param>
    /// <param name="vx">X 方向速度</param>
    /// <param name="vy">Y 方向速度</param>
    /// <param name="radius">判定半径</param>
    /// <param name="fillColor">塗りつぶし色</param>
    /// <param name="outlineColor">外枠発光色</param>
    public static void SpawnBossBullet(World world, float x, float y, float vx, float vy, float radius, Color fillColor,
        Color outlineColor)
    {
        SpawnBossSpecialBullet(world, x, y, vx, vy, radius, BulletType.Normal, fillColor, outlineColor);
    }

    /// <summary>
    ///     指定座標に特殊挙動（品紅・黄・青・黒）を持つ Boss 弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="x">出現 X 座標。</param>
    /// <param name="y">出現 Y 座標。</param>
    /// <param name="vx">X 方向速度。</param>
    /// <param name="vy">Y 方向速度。</param>
    /// <param name="radius">判定半径。</param>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="fillColor">塗りつぶし色（未指定時は種別既定色）。</param>
    /// <param name="outlineColor">外枠発光色（未指定時は種別既定色）。</param>
    public static void SpawnBossSpecialBullet(
        World world,
        float x,
        float y,
        float vx,
        float vy,
        float radius,
        BulletType type,
        Color? fillColor = null,
        Color? outlineColor = null)
    {
        var fill = fillColor ?? GetDefaultFillColor(type);
        var outline = outlineColor ?? GetDefaultOutlineColor(type);

        // 事前確保されたエンティティプールから Boss 弾幕エンティティを貸し出し
        var entity = CombatEntityPoolHelper.RentBossBullet(world)
           .Set(new Transform(x, y))
           .Set(new Velocity(vx, vy))
           .Set(new Bullet(radius, fill, outline))
           .Set(new Hitbox(new CollisionShape.Circle(radius), 1.0f, Faction.Hostile))
           .Add<BossBulletTag>();

        if (type != BulletType.Normal)
        {
            entity.Set(new BulletModifier(type, radius, vx, vy));

            if (type == BulletType.Yellow)
            {
                var cfg = ConfigManager.SpecialBullets;
                var satRadius = MathF.Max(3.0f, radius * cfg.YellowSatelliteRadiusMultiplier);
                var orbitDist = radius * cfg.YellowOrbitDistanceMultiplier;

                CombatEntityPoolHelper.RentBossBullet(world)
                   .Set(new Transform(x + orbitDist, y))
                   .Set(new Velocity(vx, vy))
                   .Set(new Bullet(satRadius, fill, outline))
                   .Set(new Hitbox(new CollisionShape.Circle(satRadius), cfg.YellowSatelliteDamage, Faction.Hostile))
                   .Set(new BulletModifier(BulletType.Yellow, satRadius, vx, vy))
                   .Set(new OrbitSatellite(entity, orbitDist))
                   .Add<BossBulletTag>();
            }
        }
        else if (entity.Has<BulletModifier>())
        {
            entity.Remove<BulletModifier>();
        }
    }

    /// <summary>
    ///     Boss 被弾時に、現在の Phase に応じた数量の黑色弾幕を全方位ランダムに発射します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="bossX">Boss の中心 X 座標。</param>
    /// <param name="bossY">Boss の中心 Y 座標。</param>
    /// <param name="phase">現在の戦闘 Phase（Phase 1: 1枚, Phase 2: 2枚, Phase 3: 3枚）。</param>
    public static void SpawnBossCounterAttack(World world, float bossX, float bossY, Phase phase)
    {
        int bulletCount;
        switch (phase)
        {
            case Phase.Phase1:
                bulletCount = 1;
            break;
            case Phase.Phase2:
                bulletCount = 2;
            break;
            case Phase.Phase3:
                bulletCount = 3;
            break;
            default:
                Debug.Fail($"[CombatEffectHelper 警告] 未知のフェーズ: {phase}");
                bulletCount = 1;
            break;
        }

        const float counterBulletSpeed = 160.0f;
        const float counterBulletRadius = 8.0f;

        for (var i = 0; i < bulletCount; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var vx = MathF.Cos(angle) * counterBulletSpeed;
            var vy = MathF.Sin(angle) * counterBulletSpeed;

            SpawnBossSpecialBullet(
                world,
                bossX,
                bossY,
                vx,
                vy,
                counterBulletRadius,
                BulletType.Black);
        }
    }

    private static Color GetDefaultFillColor(BulletType type)
    {
        return type switch {
            BulletType.Magenta => Color.FromArgb(255, 255, 20, 147),
            BulletType.Yellow => Color.FromArgb(255, 255, 215, 0),
            BulletType.Cyan => Color.FromArgb(255, 0, 240, 255),
            BulletType.Black => Color.FromArgb(255, 12, 12, 16),
            _ => ColorPalette.RedBright
        };
    }

    private static Color GetDefaultOutlineColor(BulletType type)
    {
        return type switch {
            BulletType.Magenta => Color.FromArgb(255, 255, 255, 255),
            BulletType.Yellow => Color.FromArgb(255, 255, 255, 190),
            BulletType.Cyan => Color.FromArgb(255, 255, 255, 255),
            BulletType.Black => Color.FromArgb(255, 230, 25, 75),
            _ => ColorPalette.White
        };
    }

    /// <summary>
    ///     フィールド上に存在するすべての Boss 弾幕を一括消去します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void ClearAllBossBullets(World world)
    {
        using var query = world.QueryBuilder()
           .With<BossBulletTag>()
           .Build();
        query.Each(e => CombatEntityPoolHelper.Recycle(world, e));
    }

    /// <summary>
    ///     二段ジャンプ時の空気リングエフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnJumpRingEffect(World world, float x, float y)
    {
        CombatEntityPoolHelper.RentRingEffect(world)
           .Set(new Transform(x, y))
           .Set(new RingEffect(8.0f, 32.0f, ColorPalette.White))
           .Set(new FadeEffect(0.18f));
    }

    /// <summary>
    ///     被弾時のヒットスパークエフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnHitSparkEffect(World world, float x, float y)
    {
        for (var i = 0; i < 3; i++)
        {
            var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;
            var speed = 60.0f + Random.Shared.NextSingle() * 120.0f;
            CombatEntityPoolHelper.RentSparkEffect(world)
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(3.5f, ColorPalette.SparkYellow, ColorPalette.White))
               .Set(new FadeEffect(0.12f));
        }
    }

    /// <summary>
    ///     プレイヤー死亡時の飛散パーティクルを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnPlayerDeathParticles(World world, float x, float y)
    {
        for (var i = 0; i < 16; i++)
        {
            var angle = i / 16.0f * MathF.PI * 2.0f;
            var speed = 4.0f + i % 3 * 2.0f;
            CombatEntityPoolHelper.RentSparkEffect(world)
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(5.0f, ColorPalette.Red, ColorPalette.White))
               .Set(new FadeEffect(0.60f));
        }
    }

    /// <summary>
    ///     Phase 遷移時の清屏衝撃波エフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnShockwave(World world, float x, float y)
    {
        CombatEntityPoolHelper.RentRingEffect(world)
           .Set(new Transform(x, y))
           .Set(new RingEffect(20.0f, 150.0f, ColorPalette.Cyan))
           .Set(new FadeEffect(0.40f));
    }

    /// <summary>
    ///     Boss 撃破勝利時の祝賀爆発パーティクルを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnVictoryExplosions(World world, float x, float y)
    {
        for (var i = 0; i < 32; i++)
        {
            var angle = i / 32.0f * MathF.PI * 2.0f;
            var speed = 3.0f + i % 5 * 2.5f;
            CombatEntityPoolHelper.RentSparkEffect(world)
               .Set(new Transform(x, y))
               .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
               .Set(new Bullet(6.0f, ColorPalette.Gold, ColorPalette.GoldLight))
               .Set(new FadeEffect(0.90f));
        }
    }

    /// <summary>
    ///     近接斬撃判定（矩形 Hitbox ＋ 斬撃スプライト視覚エフェクト）を生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">判定中心 X 座標</param>
    /// <param name="y">判定中心 Y 座標</param>
    /// <param name="vx">X 方向移動速度（突進追従用）</param>
    /// <param name="vy">Y 方向移動速度</param>
    /// <param name="width">判定幅</param>
    /// <param name="height">判定高さ</param>
    /// <param name="damage">与ダメージ量</param>
    /// <param name="lifetime">持続時間（秒）</param>
    /// <param name="color">斬撃エフェクトカラー</param>
    /// <param name="direction">斬撃展開方向（朝下, 朝上, 朝右, 朝左）</param>
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
        var colIndex = 5; // 第 6 列 (0-based: 5)
        var rowIndex = (int)direction;
        var srcRect = Rect.FromWidthHeight(colIndex * 64, rowIndex * 64, 64, 64);
        var scale = MathF.Max(width, height) / 28.0f;

        // 64x64 セル内の斬撃ピクセル重心偏り（Right: +18px, Left: -18px）を逆補正して視覚中心を (x, y) に一致させる
        var offsetX = direction switch {
            SlashDirection.Right => -18.0f * scale,
            SlashDirection.Left => 18.0f * scale,
            _ => 0.0f
        };
        var offsetY = direction switch {
            SlashDirection.Down => -18.0f * scale,
            SlashDirection.Up => 18.0f * scale,
            _ => 0.0f
        };

        CombatEntityPoolHelper.RentAreaEffect(world)
           .Set(new Transform(x + offsetX, y + offsetY, scale, scale))
           .Set(new Velocity(vx, vy))
           .Set(new Hitbox(new CollisionShape.Box(width, height, -offsetX, -offsetY), damage, Faction.Hostile, false))
           .Set(new Sprite(slashHandle, false, true, srcRect, color))
           .Set(new FadeEffect(lifetime));
    }

    /// <summary>
    ///     地面を這う衝撃波弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">出現 X 座標</param>
    /// <param name="y">出現 Y 座標</param>
    /// <param name="vx">水平移動速度</param>
    /// <param name="radius">判定半径</param>
    /// <param name="damage">与ダメージ量</param>
    /// <param name="fillColor">塗りつぶし色</param>
    /// <param name="outlineColor">外枠発光色</param>
    public static void SpawnGroundShockwave(World world, float x, float y, float vx, float radius, float damage,
        Color fillColor, Color outlineColor)
    {
        var entity = CombatEntityPoolHelper.RentBossBullet(world)
           .Set(new Transform(x, y))
           .Set(new Velocity(vx))
           .Set(new Bullet(radius, fillColor, outlineColor))
           .Set(new Hitbox(new CollisionShape.Circle(radius), damage, Faction.Hostile))
           .Add<BossBulletTag>();

        if (entity.Has<BulletModifier>()) entity.Remove<BulletModifier>();
    }
}