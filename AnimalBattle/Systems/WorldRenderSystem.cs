using System;
using System.Drawing;

using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using AnimalBattle.Scenes;

namespace AnimalBattle.Systems;

/// <summary>
/// アリーナ足場・タイルマップ、Boss 1 / Boss 2 (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
/// プレイヤー自機 (スプライト・無敵点滅・判定点)、および 8 方向プレイヤー弾・Boss 弾幕を描画する ECS 描画 System。
/// </summary>
public static class WorldRenderSystem
{
    /// <summary>
    /// ワールド描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddWorldRenderSystem(this World world)
    {
        Query<Transform, CollisionBox> platformQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build();

        Query<Transform, Health, Boss1Stats, Boss1StateMachine, Sprite> boss1Query =
            world.QueryBuilder<Transform, Health, Boss1Stats, Boss1StateMachine, Sprite>()

                 .With<BossTag>()
                 .With<Boss1Tag>()
                 .Build();

        Query<Transform, Health, Boss2Stats, Boss2StateMachine, Sprite> boss2Query =
            world.QueryBuilder<Transform, Health, Boss2Stats, Boss2StateMachine, Sprite>()
                 .With<BossTag>()
                 .With<Boss2Tag>()
                 .Build();

        Query<Transform, Health, PlayerState, Sprite> playerQuery =
            world.QueryBuilder<Transform, Health, PlayerState, Sprite>()
                 .With<PlayerTag>()
                 .Build();

        Query<Transform, Velocity> playerBulletQuery =
            world.QueryBuilder<Transform, Velocity>()
                 .With<PlayerBulletTag>()
                 .Build();

        Query<Transform, Bullet> bossBulletQuery =
            world.QueryBuilder<Transform, Bullet>()
                 .With<BossBulletTag>()
                 .Build();

        Query<Transform, Hitbox, HitFlashEffect, FadeEffect, Sprite> meleeSlashQuery =
            world.QueryBuilder<Transform, Hitbox, HitFlashEffect, FadeEffect, Sprite>()
                 .With<EffectTag>()
                 .Build();

        world.System("WorldRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 BossEffectsRenderConfig bossFx = renderCfg.BossEffects;
                 BulletRenderConfig bulletCfg = renderCfg.Bullets;
                 ArenaConfig arenaCfg = ConfigManager.Arena;

                 FetchPlayerPosition(playerQuery, out float playerX, out float playerY);

                 DrawPlatforms(it.World(), platformQuery);
                 DrawBoss1(boss1Query, bossFx, playerX, playerY);
                 DrawBoss2(boss2Query, bossFx, playerX, playerY);
                 DrawPlayer(playerQuery);
                 DrawPlayerBullets(playerBulletQuery, bulletCfg, arenaCfg);
                 DrawBossBullets(bossBulletQuery, bulletCfg, arenaCfg);
                 DrawMeleeEffects(meleeSlashQuery);
             });

        return world;
    }

    private static void FetchPlayerPosition(Query<Transform, Health, PlayerState, Sprite> playerQuery, out float playerX, out float playerY)
    {
        PlayerConfig pCfg = ConfigManager.Player;
        float px = pCfg.SpawnX;
        float py = pCfg.SpawnY;

        playerQuery.Each((ref Transform pTrans, ref Health pHealth, ref PlayerState pState, ref Sprite pSprite) =>
        {
            if (pHealth.IsAlive)
            {
                px = pTrans.X;
                py = pTrans.Y;
            }
        });

        playerX = px;
        playerY = py;
    }

    private static void DrawPlatforms(World world, Query<Transform, CollisionBox> platformQuery)
    {
        if (world.Has<TileMap>())
        {
            ref readonly TileMap map = ref world.Get<TileMap>();
            map.Draw();
        }
        else if (BattleScene.CurrentTileMap != null)
        {
            BattleScene.CurrentTileMap.Draw();
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

    private static void DrawBoss1(Query<Transform, Health, Boss1Stats, Boss1StateMachine, Sprite> boss1Query, BossEffectsRenderConfig bossFx, float playerX, float playerY)
    {
        boss1Query.Each((ref Transform trans, ref Health bHealth, ref Boss1Stats stats, ref Boss1StateMachine fsm, ref Sprite sprite) =>
        {
            int bx = (int)trans.X;
            int by = (int)trans.Y;

            if (fsm.CurrentState == Boss1StateKind.Telegraphing)
            {
                float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                DrawBoss1Telegraph(trans, fsm, playerX, playerY, progress);
            }

            if (sprite.IsVisible && sprite.GraphHandle >= 0 && bHealth.IsAlive)

            {
                double bossAngle = MathF.Sin(stats.HoverTimer * bossFx.BossHoverAngleSpeed) * bossFx.BossHoverAngleAmp;
                GameEngine.DrawRectRotaGraph(
                    bx,
                    by,
                    sprite.SrcRect.X1,
                    sprite.SrcRect.Y1,
                    sprite.SrcRect.Width,
                    sprite.SrcRect.Height,
                    trans.ScaleX,
                    bossAngle,
                    sprite.GraphHandle,
                    true,
                    false);

                // 隙あり（Vulnerable / Recovering）時の直接変色・脈動発光 & 醒目攻撃チャンスマーカー
                // Boss1 は冲撞（DiveRam）後隙のみ易傷エフェクトを適用
                if (fsm.CurrentState == Boss1StateKind.Recovering && fsm.CurrentSkillName == "DiveRam")
                {
                    float pulse = 0.5f + 0.5f * MathF.Sin(fsm.StateTimer * 10.0f);
                    int vulnAlpha = (int)(130 + 110 * pulse);
                    GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
                    GameEngine.DrawRectRotaGraph(
                        bx,
                        by,
                        sprite.SrcRect.X1,
                        sprite.SrcRect.Y1,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        trans.ScaleX * 1.04f,
                        bossAngle,
                        sprite.GraphHandle,
                        true,
                        false);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

                    // 頭上にボビングする醒目攻撃チャンスサイン (策划調整可能テキスト)
                    float bobbing = MathF.Sin(fsm.StateTimer * 8.0f) * 4.0f;
                    int textWidth = bossFx.ChanceMarkerText.Length * 7;
                    int textX = bx - textWidth / 2;
                    int textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
                    GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
                }



                if (stats.HitFlashTimer > 0.0f)
                {
                    GameEngine.SetDrawBlendMode(BlendMode.Add, 255);
                    GameEngine.DrawRectRotaGraph(
                        bx,
                        by,
                        sprite.SrcRect.X1,
                        sprite.SrcRect.Y1,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        trans.ScaleX,
                        bossAngle,
                        sprite.GraphHandle,
                        true,
                        false);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                }
            }
        });
    }


    private static void DrawBoss1Telegraph(Transform trans, Boss1StateMachine fsm, float playerX, float playerY, float progress)
    {
        PatternConfig patternCfg = ConfigManager.Pattern;
        string skillName = fsm.CurrentSkillName;
        float targetX = (fsm.TargetX != 0.0f || fsm.TargetY != 0.0f) ? fsm.TargetX : playerX;
        float targetY = (fsm.TargetX != 0.0f || fsm.TargetY != 0.0f) ? fsm.TargetY : playerY;

        switch (skillName)
        {
            case "Aimed3Shot":
                TelegraphRenderHelper.DrawAimingLasers(
                    trans.X,
                    trans.Y,
                    targetX,
                    targetY,
                    patternCfg.Aimed3Shot.SpreadAngle,
                    patternCfg.Aimed3Shot.BulletCount,
                    progress,
                    patternCfg.Aimed3Shot.TelegraphColor,
                    false);
                break;

            case "FastSniper":
                TelegraphRenderHelper.DrawAimingLasers(
                    trans.X,
                    trans.Y,
                    targetX,
                    targetY,
                    0.0f,
                    1,
                    progress,
                    patternCfg.FastSniper.TelegraphColor,
                    true);
                break;

            case "DiveRam":
                TelegraphRenderHelper.DrawDiveRamTelegraph(
                    trans.X,
                    trans.Y,
                    targetX,
                    targetY,
                    patternCfg.DiveRam.ImpactRadius,
                    progress,
                    patternCfg.DiveRam.TelegraphColor);
                break;

            case "RingBurst":
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    trans.X,
                    trans.Y,
                    patternCfg.RingBurst.BulletCount,
                    0.0f,
                    75.0f,
                    progress,
                    patternCfg.RingBurst.TelegraphColor);
                break;

            case "DoubleSpiral":
                float spiralAngle = fsm.StateTimer * 4.0f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    trans.X,
                    trans.Y,
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
                float crossAngle = fsm.StateTimer * 2.5f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    trans.X,
                    trans.Y,
                    4,
                    crossAngle,
                    110.0f,
                    progress,
                    patternCfg.CrossChaos.TelegraphColor);
                break;

            default:
                TelegraphRenderHelper.DrawAimingLasers(
                    trans.X,
                    trans.Y,
                    targetX,
                    targetY,
                    patternCfg.Aimed3Shot.SpreadAngle,
                    patternCfg.Aimed3Shot.BulletCount,
                    progress,
                    patternCfg.Aimed3Shot.TelegraphColor,
                    false);
                break;

        }
    }

    private static void DrawBoss2(Query<Transform, Health, Boss2Stats, Boss2StateMachine, Sprite> boss2Query, BossEffectsRenderConfig bossFx, float playerX, float playerY)
    {
        boss2Query.Each((ref Transform trans, ref Health bHealth, ref Boss2Stats stats, ref Boss2StateMachine fsm, ref Sprite sprite) =>
        {
            int bx = (int)trans.X;
            int by = (int)trans.Y;

            if (fsm.CurrentState is Boss2StateKind.TelegraphHeavySlash
                or Boss2StateKind.TelegraphDoubleCleave
                or Boss2StateKind.TelegraphGroundStomp
                or Boss2StateKind.TelegraphLeapSmash
                or Boss2StateKind.TelegraphFrenzyCharge
                or Boss2StateKind.TelegraphEarthquakeRoar)
            {
                float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                DrawBoss2Telegraph(trans, fsm, playerX, playerY, !sprite.FlipX, progress);
            }

            if (sprite.IsVisible && sprite.GraphHandle >= 0 && bHealth.IsAlive)

            {
                GameEngine.DrawRectRotaGraph(
                    bx,
                    by,
                    sprite.SrcRect.X1,
                    sprite.SrcRect.Y1,
                    sprite.SrcRect.Width,
                    sprite.SrcRect.Height,
                    trans.ScaleX,
                    0.0,
                    sprite.GraphHandle,
                    true,
                    sprite.FlipX);

                // 隙あり（Vulnerable）時の直接変色・脈動発光 & 醒目攻撃チャンスマーカー
                // Boss2 は泰山圧頂（LeapSmash）着地後隙のみ易傷エフェクトを適用
                if (fsm.CurrentState == Boss2StateKind.SkillRecovery && fsm.CurrentSkillName == "LeapSmash")
                {
                    float pulse = 0.5f + 0.5f * MathF.Sin(fsm.StateTimer * 10.0f);
                    int vulnAlpha = (int)(130 + 110 * pulse);
                    GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
                    GameEngine.DrawRectRotaGraph(
                        bx,
                        by,
                        sprite.SrcRect.X1,
                        sprite.SrcRect.Y1,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        trans.ScaleX * 1.04f,
                        0.0,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

                    // 頭上にボビングする醒目攻撃チャンスサイン (策划調整可能テキスト)
                    float bobbing = MathF.Sin(fsm.StateTimer * 8.0f) * 4.0f;
                    int textWidth = bossFx.ChanceMarkerText.Length * 7;
                    int textX = bx - textWidth / 2;
                    int textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
                    GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
                }

                if (stats.HitFlashTimer > 0.0f)
                {
                    GameEngine.SetDrawBlendMode(BlendMode.Add, 255);
                    GameEngine.DrawRectRotaGraph(
                        bx,
                        by,
                        sprite.SrcRect.X1,
                        sprite.SrcRect.Y1,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        trans.ScaleX,
                        0.0,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                }
            }
        });
    }

    private static void DrawBoss2Telegraph(Transform trans, Boss2StateMachine fsm, float playerX, float playerY, bool facingRight, float progress)
    {
        Boss2Config b2Cfg = ConfigManager.Boss2;
        PatternConfig patternCfg = ConfigManager.Pattern;
        int bx = (int)trans.X;
        int by = (int)trans.Y;
        float targetX = (fsm.TargetX != 0.0f || fsm.TargetY != 0.0f) ? fsm.TargetX : playerX;
        float targetY = (fsm.TargetX != 0.0f || fsm.TargetY != 0.0f) ? fsm.TargetY : playerY;

        switch (fsm.CurrentState)
        {
            case Boss2StateKind.TelegraphGroundStomp:
                float groundY = trans.Y + b2Cfg.CollisionHeight / 2.0f;
                TelegraphRenderHelper.DrawGroundShockwaveRails(bx, groundY, progress, patternCfg.GroundStomp.TelegraphColor);
                break;

            case Boss2StateKind.TelegraphLeapSmash:
                TelegraphRenderHelper.DrawLeapSmashTelegraph(
                    targetX,
                    targetY,
                    progress,
                    patternCfg.LeapSmash.TelegraphColor);
                break;


            case Boss2StateKind.TelegraphEarthquakeRoar:
                TelegraphRenderHelper.DrawSkyfallColumns(
                    patternCfg.EarthquakeRoar.MinX,
                    patternCfg.EarthquakeRoar.MaxX,
                    5,
                    650.0f,
                    progress,
                    patternCfg.EarthquakeRoar.TelegraphColor,
                    "▼ ROCK ▼");
                break;

            case Boss2StateKind.TelegraphFrenzyCharge:
                TelegraphRenderHelper.DrawChargeDangerTrack(
                    bx,
                    by,
                    b2Cfg.CollisionHeight,
                    facingRight,
                    progress,
                    patternCfg.FrenzyCharge.TelegraphColor);
                break;

            case Boss2StateKind.TelegraphDoubleCleave:
                TelegraphRenderHelper.DrawMeleeDangerBox(
                    bx,
                    by,
                    patternCfg.DoubleCleave.SlashRangeX,
                    patternCfg.DoubleCleave.SlashRangeY,
                    facingRight,
                    progress,
                    patternCfg.DoubleCleave.TelegraphColor);
                break;

            case Boss2StateKind.TelegraphHeavySlash:
            default:
                TelegraphRenderHelper.DrawMeleeDangerBox(
                    bx,
                    by,
                    patternCfg.HeavySlash.SlashRangeX,
                    patternCfg.HeavySlash.SlashRangeY,
                    facingRight,
                    progress,
                    patternCfg.HeavySlash.TelegraphColor);
                break;
        }
    }

    private static void DrawPlayer(Query<Transform, Health, PlayerState, Sprite> playerQuery)
    {
        playerQuery.Each((ref Transform trans, ref Health health, ref PlayerState playerState, ref Sprite sprite) =>
        {
            if (!health.IsAlive || !sprite.IsVisible || sprite.GraphHandle < 0)
            {
                return;
            }

            bool shouldDrawSprite = !health.IsInvincible || ((int)(health.InvincibleTimer * 30.0f) % 2 == 0);

            if (shouldDrawSprite)
            {
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
            }
        });
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
        int pTrailRadius = bulletCfg.PlayerBulletTrailHalfHeight * 2;
        Color pTrailColor = bulletCfg.PlayerBulletTrailColor;

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
                float lenSq = vx * vx + vy * vy;
                if (lenSq > 0.0001f)
                {
                    float invLen = 1.0f / MathF.Sqrt(lenSq);
                    int tx = (int)(px - (vx * invLen) * pTrailOffset);
                    int ty = (int)(py - (vy * invLen) * pTrailOffset);
                    GameEngine.DrawCircle(tx, ty, pTrailRadius, pTrailColor, true);
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

    private static void DrawMeleeEffects(Query<Transform, Hitbox, HitFlashEffect, FadeEffect, Sprite> meleeSlashQuery)
    {
        meleeSlashQuery.Each((ref Transform trans, ref Hitbox hit, ref HitFlashEffect flash, ref FadeEffect fade, ref Sprite sprite) =>
        {
            int alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
            if (alpha <= 0)
            {
                return;
            }

            if (sprite.IsVisible && sprite.GraphHandle >= 0)
            {
                GameEngine.SetDrawBright(flash.FlashColor.R, flash.FlashColor.G, flash.FlashColor.B);
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
                GameEngine.DrawBox(x1, y1, x2, y2, flash.FlashColor, true);
                GameEngine.DrawBox(x1 - 2, y1 - 2, x2 + 2, y2 + 2, ColorPalette.White, false);
                GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
            }
        });
    }
}
