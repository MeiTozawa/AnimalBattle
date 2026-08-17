using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 戦闘演出（被弾スパーク・ジャンプエフェクト・衝撃波・撃破爆発等）および弾幕生成の補助メソッド群を提供する静的ヘルパークラス。
/// すべてのデフォルトカラーに <see cref="ColorPalette"/> を使用します。
/// </summary>
public static class CombatEffectHelper
{
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
             .Add<BossBulletTag>();
    }

    /// <summary>
    /// フィールド上に存在するすべての Boss 弾幕を一括消去します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void ClearAllBossBullets(World world)
    {
        world.QueryBuilder()
             .With<BossBulletTag>()
             .Build()
             .Each((Entity e) => e.Destruct());
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
             .Set(FadeEffect.FadeOut(0.18f))
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
             .Set(FadeEffect.FadeOut(0.12f))
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
             .Set(FadeEffect.FadeOut(0.08f))
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
                 .Set(FadeEffect.FadeOut(0.60f))
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
             .Set(FadeEffect.FadeOut(0.40f))
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
                 .Set(FadeEffect.FadeOut(0.90f))
                 .Add<EffectTag>();
        }
    }
}
