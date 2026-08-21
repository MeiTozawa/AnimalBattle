using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;

namespace AnimalBattle.Systems;

/// <summary>
/// 戦闘演出（被弾スパーク・ジャンプエフェクト・衝撃波・撃破爆発等）および弾幕生成の補助メソッド群を提供する静的ヘルパークラス。
/// 生成する敵弾に <see cref="Hitbox"/>（敵性陣営・円形判定）を付与します。
/// </summary>
public static class CombatEffectHelper
{
    private static Query _bossBulletQuery;

    /// <summary>
    /// ヘルパークラスで使用する事前構築クエリを初期化します。
    /// </summary>
    /// <param name="world">初期化対象の Flecs.NET ワールド</param>
    public static void Initialize(World world)
    {
        _bossBulletQuery = world.QueryBuilder()
                                .With<BossBulletTag>()
                                .Build();
    }

    /// <summary>
    /// 指定座標に Boss 弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">出現 X 座標</param>
    /// <param name="y">出現 Y 座標</param>
    /// <param name="vx">X 方向速度</param>
    /// <param name="vy">Y 方向速度</param>
    /// <param name="radius">判定半径</param>
    /// <param name="fillColor">塗りつぶし色</param>
    /// <param name="outlineColor">外枠発光色</param>
    public static void SpawnBossBullet(World world, float x, float y, float vx, float vy, float radius, Color fillColor, Color outlineColor)
    {
        world.Entity()
             .Set(new Transform(x, y))
             .Set(new Velocity(vx, vy))
             .Set(new Bullet(radius, fillColor, outlineColor))
             .Set(new Hitbox(new CollisionShape.Circle(radius), damage: 1.0f, faction: Faction.Hostile, destroyOnHit: true))
             .Add<BossBulletTag>();
    }

    /// <summary>
    /// フィールド上に存在するすべての Boss 弾幕を一括消去します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void ClearAllBossBullets(World world)
    {
        unsafe
        {
            if (_bossBulletQuery.Handle != null)
            {
                _bossBulletQuery.Each((Entity e) => e.Destruct());
            }
        }
    }

    /// <summary>
    /// 二段ジャンプ時の空気リングエフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnJumpRingEffect(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 1.0f, scaleY: 1.0f))
             .Set(new FadeEffect(0.18f))
             .Add<EffectTag>();
    }

    /// <summary>
    /// 被弾時のヒットスパークエフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnHitSparkEffect(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 1.5f, scaleY: 1.5f))
             .Set(new FadeEffect(0.12f))
             .Set(new HitFlashEffect(0.08f, ColorPalette.SparkYellow))
             .Add<EffectTag>();
    }

    /// <summary>
    /// Boss 技能予兆時のチャージスパークエフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    /// <param name="color">予兆カラー</param>
    public static void SpawnTelegraphSpark(World world, float x, float y, Color color)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 2.2f, scaleY: 2.2f))
             .Set(new FadeEffect(0.08f))
             .Set(new HitFlashEffect(0.06f, color))
             .Add<EffectTag>();
    }

    /// <summary>
    /// プレイヤー死亡時の飛散パーティクルを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnPlayerDeathParticles(World world, float x, float y)
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = (i / 16.0f) * MathF.PI * 2.0f;
            float speed = 4.0f + (i % 3) * 2.0f;
            world.Entity()
                 .Set(new Transform(x, y))
                 .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
                 .Set(new Bullet(5.0f, ColorPalette.Red, ColorPalette.White))
                 .Set(new FadeEffect(0.60f))
                 .Add<EffectTag>();
        }
    }

    /// <summary>
    /// Phase 遷移時の清屏衝撃波エフェクトを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnShockwave(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 3.5f, scaleY: 3.5f))
             .Set(new FadeEffect(0.40f))
             .Add<EffectTag>();
    }

    /// <summary>
    /// Boss 撃破勝利時の祝賀爆発パーティクルを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    public static void SpawnVictoryExplosions(World world, float x, float y)
    {
        for (int i = 0; i < 32; i++)
        {
            float angle = (i / 32.0f) * MathF.PI * 2.0f;
            float speed = 3.0f + (i % 5) * 2.5f;
            world.Entity()
                 .Set(new Transform(x, y))
                 .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
                 .Set(new Bullet(6.0f, ColorPalette.Gold, ColorPalette.GoldLight))
                 .Set(new FadeEffect(0.90f))
                 .Add<EffectTag>();
        }
    }

    /// <summary>
    /// 近接斬撃判定（矩形 Hitbox ＋ 斬撃スプライト視覚エフェクト）を生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">判定中心 X 座標</param>
    /// <param name="y">判定中心 Y 座標</param>
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
        float width,
        float height,
        float damage,
        float lifetime,
        Color color,
        SlashDirection direction = SlashDirection.Right)
    {
        int slashHandle = AssetManager.GetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng);
        int colIndex = 5; // 第 6 列 (0-based: 5)
        int rowIndex = (int)direction;
        Rect<int> srcRect = new(colIndex * 64, rowIndex * 64, 64, 64);
        float scaleX = width / 40.0f;
        float scaleY = height / 40.0f;

        world.Entity()
             .Set(new Transform(x, y, scaleX: scaleX, scaleY: scaleY))
             .Set(new Hitbox(new CollisionShape.Box(width, height), damage: damage, faction: Faction.Hostile, destroyOnHit: false))
             .Set(new Sprite(slashHandle, flipX: false, isVisible: true, srcRect: srcRect))
             .Set(new FadeEffect(lifetime))
             .Set(new HitFlashEffect(lifetime, color))
             .Add<EffectTag>();
    }

    /// <summary>
    /// 地面を這う衝撃波弾幕エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="x">出現 X 座標</param>
    /// <param name="y">出現 Y 座標</param>
    /// <param name="vx">水平移動速度</param>
    /// <param name="radius">判定半径</param>
    /// <param name="damage">与ダメージ量</param>
    /// <param name="fillColor">塗りつぶし色</param>
    /// <param name="outlineColor">外枠発光色</param>
    public static void SpawnGroundShockwave(World world, float x, float y, float vx, float radius, float damage, Color fillColor, Color outlineColor)
    {
        world.Entity()
             .Set(new Transform(x, y))
             .Set(new Velocity(vx, 0.0f))
             .Set(new Bullet(radius, fillColor, outlineColor))
             .Set(new Hitbox(new CollisionShape.Circle(radius), damage: damage, faction: Faction.Hostile, destroyOnHit: true))
             .Add<BossBulletTag>();
    }
}
