using System;
using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// アリーナ足場・タイルマップ、Boss 1 / Boss 2 (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
/// プレイヤー自機 (スプライト・無敵点滅)、および 8 方向プレイヤー弾・Boss 弾幕・近接斬撃エフェクトを描画する世界描画システム。
/// 領域モデル（<see cref="Player"/> / <see cref="BossCharacter"/>）から直接キャラクターを描画し、
/// 海量弾幕・パーティクルエフェクトのみを Flecs ECS 高速クエリで一括描画します。
/// </summary>
public sealed class WorldRenderSystem : IDisposable
{
    private readonly Query<Transform, Velocity> _playerBulletQuery;
    private readonly Query<Transform, Bullet> _bossBulletQuery;
    private readonly Query<Transform, Hitbox, FadeEffect, Sprite> _meleeSlashQuery;
    private readonly Query<Transform, Sprite, HitFlashEffect> _hitFlashQuery;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="WorldRenderSystem"/> の新しいインスタンスを初期化し、弾幕・エフェクト用のクエリを構築します。
    /// </summary>
    /// <param name="world">描画対象の Flecs.NET ワールド。</param>
    public WorldRenderSystem(World world)
    {
        _playerBulletQuery = world.QueryBuilder<Transform, Velocity>()
                                  .With<PlayerBulletTag>()
                                  .Build();

        _bossBulletQuery = world.QueryBuilder<Transform, Bullet>()
                                .With<BossBulletTag>()
                                .Build();

        _meleeSlashQuery = world.QueryBuilder<Transform, Hitbox, FadeEffect, Sprite>()
                                .With<EffectTag>()
                                .Build();

        _hitFlashQuery = world.QueryBuilder<Transform, Sprite, HitFlashEffect>()
                              .Build();
    }

    /// <summary>
    /// 世界の全ゲームオブジェクト（マップ、Boss、自機、弾幕、エフェクト）を描画します。
    /// </summary>
    /// <param name="tileMap">現在のアリーナタイルマップ。</param>
    /// <param name="platformQuery">プラットフォーム足場検索クエリ。</param>
    /// <param name="player">現在アクティブなプレイヤー自機。</param>
    /// <param name="boss">現在アクティブな Boss。</param>
    public void Render(
        TileMap? tileMap,
        Query<Transform, CollisionBox> platformQuery,
        Player? player,
        BossCharacter? boss)
    {
        RenderConfig renderCfg = ConfigManager.Render;
        BossEffectsRenderConfig bossFx = renderCfg.BossEffects;
        BulletRenderConfig bulletCfg = renderCfg.Bullets;
        ArenaConfig arenaCfg = ConfigManager.Arena;

        float playerX = player != null ? player.X : ConfigManager.Player.SpawnX;
        float playerY = player != null ? player.Y : ConfigManager.Player.SpawnY;

        DrawPlatforms(tileMap, platformQuery);
        DrawBoss(boss, bossFx, playerX, playerY);
        DrawPlayer(player);
        DrawHitFlashEffects(_hitFlashQuery);
        DrawPlayerBullets(_playerBulletQuery, bulletCfg, arenaCfg);
        DrawBossBullets(_bossBulletQuery, bulletCfg, arenaCfg);
        DrawMeleeEffects(_meleeSlashQuery);
    }

    private static void DrawPlatforms(TileMap? tileMap, Query<Transform, CollisionBox> platformQuery)
    {
        if (tileMap != null)
        {
            tileMap.Draw();
        }
        else
        {
            platformQuery.Each((ref Transform trans, ref CollisionBox col) =>
            {
                int x1 = (int)(trans.X - col.Width / 2.0f);
                int y1 = (int)(trans.Y - col.Height / 2.0f);
                int x2 = (int)(trans.X + col.Width / 2.0f);
                int y2 = (int)(trans.Y + col.Height / 2.0f);

                GameEngine.DrawBox(x1, y1, x2, y2, ColorPalette.DarkBg, true);
                GameEngine.DrawBox(x1, y1, x2, y2, ColorPalette.Cyan, false);
            });
        }
    }

    private static void DrawBoss(BossCharacter? boss, BossEffectsRenderConfig bossFx, float playerX, float playerY)
    {
        if (boss == null || !boss.IsAlive || !boss.Sprite.IsVisible || boss.Sprite.GraphHandle < 0)
        {
            return;
        }

        int bx = (int)boss.X;
        int by = (int)boss.Y;

        if (boss.BossType == BossType.Boss1)
        {
            if (boss.CurrentState == BossState.Boss1_Telegraph)
            {
                float progress = Math.Clamp(boss.StateTimer / Math.Max(0.01f, boss.StateDuration), 0.0f, 1.0f);
                DrawBoss1Telegraph(boss.X, boss.Y, boss.SkillName, boss.TargetX, boss.TargetY, playerX, playerY, progress, boss.StateTimer);
            }

            double bossAngle = MathF.Sin(boss.HoverTimer * bossFx.BossHoverAngleSpeed) * bossFx.BossHoverAngleAmp;
            GameEngine.DrawRectRotaGraph(
                bx,
                by,
                boss.Sprite.SrcRect.X1,
                boss.Sprite.SrcRect.Y1,
                boss.Sprite.SrcRect.Width,
                boss.Sprite.SrcRect.Height,
                boss.ScaleX,
                bossAngle,
                boss.Sprite.GraphHandle,
                true,
                false);

            // 隙あり（Vulnerable）時の直接変色・脈動発光 & 攻撃チャンスマーカー
            if (boss.IsVulnerable)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(boss.StateTimer * 10.0f);
                int vulnAlpha = (int)(130 + 110 * pulse);
                GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
                GameEngine.DrawRectRotaGraph(
                    bx,
                    by,
                    boss.Sprite.SrcRect.X1,
                    boss.Sprite.SrcRect.Y1,
                    boss.Sprite.SrcRect.Width,
                    boss.Sprite.SrcRect.Height,
                    boss.ScaleX * 1.04f,
                    bossAngle,
                    boss.Sprite.GraphHandle,
                    true,
                    false);
                GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

                float bobbing = MathF.Sin(boss.StateTimer * 8.0f) * 4.0f;
                int textWidth = bossFx.ChanceMarkerText.Length * 7;
                int textX = bx - textWidth / 2;
                int textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
                GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
            }
        }
        else
        {
            if (boss.CurrentState == BossState.Boss2_Telegraph)
            {
                float progress = Math.Clamp(boss.StateTimer / Math.Max(0.01f, boss.StateDuration), 0.0f, 1.0f);
                DrawBoss2Telegraph(boss.X, boss.Y, boss.CurrentSkill, boss.TargetX, boss.TargetY, playerX, playerY, boss.FacingRight, progress);
            }

            bool flipX = !boss.FacingRight;
            GameEngine.DrawRectRotaGraph(
                bx,
                by,
                boss.Sprite.SrcRect.X1,
                boss.Sprite.SrcRect.Y1,
                boss.Sprite.SrcRect.Width,
                boss.Sprite.SrcRect.Height,
                boss.ScaleX,
                0.0,
                boss.Sprite.GraphHandle,
                true,
                flipX);

            // 隙あり（Vulnerable）時の直接変色・脈動発光 & 攻撃チャンスマーカー
            if (boss.IsVulnerable)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(boss.StateTimer * 10.0f);
                int vulnAlpha = (int)(130 + 110 * pulse);
                GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
                GameEngine.DrawRectRotaGraph(
                    bx,
                    by,
                    boss.Sprite.SrcRect.X1,
                    boss.Sprite.SrcRect.Y1,
                    boss.Sprite.SrcRect.Width,
                    boss.Sprite.SrcRect.Height,
                    boss.ScaleX * 1.04f,
                    0.0,
                    boss.Sprite.GraphHandle,
                    true,
                    flipX);
                GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

                float bobbing = MathF.Sin(boss.StateTimer * 8.0f) * 4.0f;
                int textWidth = bossFx.ChanceMarkerText.Length * 7;
                int textX = bx - textWidth / 2;
                int textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
                GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
            }
        }
    }

    private static void DrawBoss1Telegraph(
        float bx,
        float by,
        string skillName,
        float targetX,
        float targetY,
        float playerX,
        float playerY,
        float progress,
        float stateTimer)
    {
        PatternConfig patternCfg = ConfigManager.Pattern;
        float finalTargetX = (targetX != 0.0f || targetY != 0.0f) ? targetX : playerX;
        float finalTargetY = (targetX != 0.0f || targetY != 0.0f) ? targetY : playerY;

        switch (skillName)
        {
            case "Aimed3Shot":
                TelegraphRenderHelper.DrawAimingLasers(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    patternCfg.Aimed3Shot.SpreadAngle,
                    patternCfg.Aimed3Shot.BulletCount,
                    progress,
                    patternCfg.Aimed3Shot.TelegraphColor,
                    false);
                break;

            case "FastSniper":
                TelegraphRenderHelper.DrawAimingLasers(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    0.0f,
                    1,
                    progress,
                    patternCfg.FastSniper.TelegraphColor,
                    true);
                break;

            case "DiveRam":
                TelegraphRenderHelper.DrawDiveRamTelegraph(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    patternCfg.DiveRam.ImpactRadius,
                    progress,
                    patternCfg.DiveRam.TelegraphColor);
                break;

            case "RingBurst":
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    bx,
                    by,
                    patternCfg.RingBurst.BulletCount,
                    0.0f,
                    75.0f,
                    progress,
                    patternCfg.RingBurst.TelegraphColor);
                break;

            case "DoubleSpiral":
                float spiralAngle = stateTimer * 4.0f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    bx,
                    by,
                    2,
                    spiralAngle,
                    90.0f,
                    progress,
                    patternCfg.DoubleSpiral.TelegraphColor);
                break;

            case "RainDrop":
                TelegraphRenderHelper.DrawSkyfallColumns(
                    patternCfg.RainDrop.MinX,
                    patternCfg.RainDrop.MaxX,
                    6,
                    650.0f,
                    progress,
                    patternCfg.RainDrop.TelegraphColor,
                    "▼ RAIN ▼");
                break;

            case "CrossChaos":
                float crossAngle = stateTimer * 2.5f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    bx,
                    by,
                    4,
                    crossAngle,
                    110.0f,
                    progress,
                    patternCfg.CrossChaos.TelegraphColor);
                break;

            default:
                TelegraphRenderHelper.DrawAimingLasers(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    patternCfg.Aimed3Shot.SpreadAngle,
                    patternCfg.Aimed3Shot.BulletCount,
                    progress,
                    patternCfg.Aimed3Shot.TelegraphColor,
                    false);
                break;
        }
    }

    private static void DrawBoss2Telegraph(
        float bx,
        float by,
        BossSkill skill,
        float targetX,
        float targetY,
        float playerX,
        float playerY,
        bool facingRight,
        float progress)
    {
        Boss2Config b2Cfg = ConfigManager.Boss2;
        PatternConfig patternCfg = ConfigManager.Pattern;
        float finalTargetX = (targetX != 0.0f || targetY != 0.0f) ? targetX : playerX;
        float finalTargetY = (targetX != 0.0f || targetY != 0.0f) ? targetY : playerY;

        switch (skill)
        {
            case BossSkill.GroundStomp:
                float groundY = by + b2Cfg.CollisionHeight / 2.0f;
                TelegraphRenderHelper.DrawGroundShockwaveRails((int)bx, groundY, progress, patternCfg.GroundStomp.TelegraphColor);
                break;

            case BossSkill.LeapSmash:
                TelegraphRenderHelper.DrawLeapSmashTelegraph(
                    finalTargetX,
                    finalTargetY,
                    progress,
                    patternCfg.LeapSmash.TelegraphColor);
                break;

            case BossSkill.EarthquakeRoar:
                TelegraphRenderHelper.DrawSkyfallColumns(
                    patternCfg.EarthquakeRoar.MinX,
                    patternCfg.EarthquakeRoar.MaxX,
                    5,
                    650.0f,
                    progress,
                    patternCfg.EarthquakeRoar.TelegraphColor,
                    "▼ ROCK ▼");
                break;

            case BossSkill.FrenzyCharge:
                TelegraphRenderHelper.DrawChargeDangerTrack(
                    (int)bx,
                    (int)by,
                    b2Cfg.CollisionHeight,
                    facingRight,
                    progress,
                    patternCfg.FrenzyCharge.TelegraphColor);
                break;

            case BossSkill.DoubleCleave:
                TelegraphRenderHelper.DrawMeleeDangerBox(
                    (int)bx,
                    (int)by,
                    patternCfg.DoubleCleave.SlashRangeX,
                    patternCfg.DoubleCleave.SlashRangeY,
                    facingRight,
                    progress,
                    patternCfg.DoubleCleave.TelegraphColor);
                break;

            case BossSkill.HeavySlash:
            default:
                TelegraphRenderHelper.DrawMeleeDangerBox(
                    (int)bx,
                    (int)by,
                    patternCfg.HeavySlash.SlashRangeX,
                    patternCfg.HeavySlash.SlashRangeY,
                    facingRight,
                    progress,
                    patternCfg.HeavySlash.TelegraphColor);
                break;
        }
    }

    private static void DrawPlayer(Player? player)
    {
        if (player == null || !player.IsAlive || !player.Sprite.IsVisible || player.Sprite.GraphHandle < 0)
        {
            return;
        }

        bool isFlashing = player.IsFlashing;
        bool isInvincibleBlinking = !isFlashing && player.InvincibleTimer > 0.0f && ((int)(player.InvincibleTimer * 30.0f) % 2 != 0);

        if (!isInvincibleBlinking)
        {
            GameEngine.DrawRectRotaGraph(
                (int)player.X,
                (int)player.Y,
                player.Sprite.SrcRect.X1,
                player.Sprite.SrcRect.Y1,
                player.Sprite.SrcRect.Width,
                player.Sprite.SrcRect.Height,
                player.ScaleX,
                0.0,
                player.Sprite.GraphHandle,
                true,
                player.Sprite.FlipX);
        }
    }

    private static void DrawPlayerBullets(Query<Transform, Velocity> playerBulletQuery, BulletRenderConfig bulletCfg, ArenaConfig arenaCfg)
    {
        int clipMinX = (int)arenaCfg.BulletDespawnMinX - bulletCfg.ViewportCullingMarginX;
        int clipMaxX = (int)arenaCfg.BulletDespawnMaxX + bulletCfg.ViewportCullingMarginX;
        int clipMinY = (int)arenaCfg.BulletDespawnMinY - bulletCfg.ViewportCullingMarginY;
        int clipMaxY = (int)arenaCfg.BulletDespawnMaxY + bulletCfg.ViewportCullingMarginY;

        int pOuterRadius = (int)bulletCfg.PlayerBulletOuterRadius;
        Color pOuterColor = bulletCfg.PlayerBulletOuterColor;
        int pInnerRadius = (int)bulletCfg.PlayerBulletInnerRadius;
        Color pInnerColor = bulletCfg.PlayerBulletInnerColor;
        int pTrailOffset = bulletCfg.PlayerBulletTrailHalfWidth;

        playerBulletQuery.Iter((Iter it, Field<Transform> trans, Field<Velocity> vel) =>
        {
            int count = it.Count();
            for (int i = 0; i < count; ++i)
            {
                int px = (int)trans[i].X;
                int py = (int)trans[i].Y;
                if (px < clipMinX || px > clipMaxX || py < clipMinY || py > clipMaxY)
                {
                    continue;
                }

                float vx = vel[i].Vx;
                float vy = vel[i].Vy;
                float speedSq = vx * vx + vy * vy;
                if (speedSq > 1.0f)
                {
                    float invSpeed = 1.0f / MathF.Sqrt(speedSq);
                    float dirX = vx * invSpeed;
                    float dirY = vy * invSpeed;

                    int trailX = (int)(px - dirX * pTrailOffset);
                    int trailY = (int)(py - dirY * pTrailOffset);
                    GameEngine.DrawCircle(trailX, trailY, pInnerRadius, pOuterColor, true);
                }

                GameEngine.DrawCircle(px, py, pOuterRadius, pOuterColor, true);
                GameEngine.DrawCircle(px, py, pInnerRadius, pInnerColor, true);
            }
        });
    }

    private static void DrawBossBullets(Query<Transform, Bullet> bossBulletQuery, BulletRenderConfig bulletCfg, ArenaConfig arenaCfg)
    {
        int clipMinX = (int)arenaCfg.BulletDespawnMinX - bulletCfg.ViewportCullingMarginX;
        int clipMaxX = (int)arenaCfg.BulletDespawnMaxX + bulletCfg.ViewportCullingMarginX;
        int clipMinY = (int)arenaCfg.BulletDespawnMinY - bulletCfg.ViewportCullingMarginY;
        int clipMaxY = (int)arenaCfg.BulletDespawnMaxY + bulletCfg.ViewportCullingMarginY;

        int outlinePadding = bulletCfg.BossBulletOutlinePadding;
        float coreRadiusRatio = bulletCfg.BossBulletCoreRadiusRatio;
        int coreMinRadius = bulletCfg.BossBulletCoreMinRadius;
        Color coreColor = bulletCfg.BossBulletCoreColor;

        bossBulletQuery.Iter((Iter it, Field<Transform> trans, Field<Bullet> bullet) =>
        {
            int count = it.Count();
            for (int i = 0; i < count; ++i)
            {
                int bx = (int)trans[i].X;
                int by = (int)trans[i].Y;
                if (bx < clipMinX || bx > clipMaxX || by < clipMinY || by > clipMaxY)
                {
                    continue;
                }

                Bullet b = bullet[i];
                int radius = (int)b.Radius;
                int outlineRadius = radius + outlinePadding;
                int coreRadius = Math.Max(coreMinRadius, (int)(radius * coreRadiusRatio));

                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
            }
        });
    }

    private static void DrawHitFlashEffects(Query<Transform, Sprite, HitFlashEffect> hitFlashQuery)
    {
        hitFlashQuery.Each((ref Transform trans, ref Sprite sprite, ref HitFlashEffect flash) =>
        {
            if (!sprite.IsVisible || sprite.GraphHandle < 0)
            {
                return;
            }

            int alpha = (int)(255 * flash.Intensity);
            if (alpha <= 0)
            {
                return;
            }

            GameEngine.SetDrawBright(flash.FlashColor.R, flash.FlashColor.G, flash.FlashColor.B);
            GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
            GameEngine.DrawRectRotaGraph(
                (int)trans.X,
                (int)trans.Y,
                sprite.SrcRect.Width > 0 ? sprite.SrcRect.X1 : 0,
                sprite.SrcRect.Height > 0 ? sprite.SrcRect.Y1 : 0,
                sprite.SrcRect.Width,
                sprite.SrcRect.Height,
                trans.ScaleX,
                trans.Rotation,
                sprite.GraphHandle,
                true,
                sprite.FlipX);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
            GameEngine.SetDrawBright(255, 255, 255);
        });
    }

    private static void DrawMeleeEffects(Query<Transform, Hitbox, FadeEffect, Sprite> meleeSlashQuery)
    {
        meleeSlashQuery.Each((ref Transform trans, ref Hitbox hit, ref FadeEffect fade, ref Sprite sprite) =>
        {
            int alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
            if (alpha <= 0)
            {
                return;
            }

            if (sprite.IsVisible && sprite.GraphHandle >= 0)
            {
                GameEngine.SetDrawBright(sprite.Color.R, sprite.Color.G, sprite.Color.B);
                GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
                GameEngine.DrawRectRotaGraph(
                    (int)trans.X,
                    (int)trans.Y,
                    sprite.SrcRect.X1,
                    sprite.SrcRect.Y1,
                    sprite.SrcRect.Width,
                    sprite.SrcRect.Height,
                    trans.ScaleX,
                    trans.Rotation,
                    sprite.GraphHandle,
                    true,
                    sprite.FlipX);
                GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                GameEngine.SetDrawBright(255, 255, 255);
            }
            else if (hit.Shape is CollisionShape.Box box)
            {
                int x1 = (int)(trans.X - box.Width / 2.0f);
                int y1 = (int)(trans.Y - box.Height / 2.0f);
                int x2 = (int)(trans.X + box.Width / 2.0f);
                int y2 = (int)(trans.Y + box.Height / 2.0f);

                GameEngine.SetDrawBlendMode(BlendMode.Add, (int)(alpha * 0.8f));
                GameEngine.DrawBox(x1, y1, x2, y2, sprite.Color, true);
                GameEngine.DrawBox(x1 - 2, y1 - 2, x2 + 2, y2 + 2, ColorPalette.White, false);
                GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _playerBulletQuery.Dispose();
            _bossBulletQuery.Dispose();
            _meleeSlashQuery.Dispose();
            _hitFlashQuery.Dispose();
            _isDisposed = true;
        }
    }
}
