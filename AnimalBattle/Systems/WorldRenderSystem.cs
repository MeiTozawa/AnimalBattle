using System;
using System.Drawing;
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

                 if (it.World().Has<TileMap>())
                 {
                     ref readonly TileMap map = ref it.World().Get<TileMap>();
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

                 boss1Query.Each((ref Transform trans, ref Health bHealth, ref Boss1Stats stats, ref Boss1StateMachine fsm, ref Sprite sprite) =>
                 {
                     int bx = (int)trans.X;
                     int by = (int)trans.Y;

                     if (fsm.CurrentState == Boss1StateKind.Telegraphing)
                     {
                         float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                         int ringRadius = (int)(bossFx.TelegraphStartRadius - progress * bossFx.TelegraphEndRadiusDelta);
                         GameEngine.DrawCircle(bx, by, ringRadius, bossFx.TelegraphRingColor, false);
                         GameEngine.DrawCircle(bx, by, ringRadius - 2, bossFx.TelegraphInnerRingColor, false);
                         GameEngine.DrawCircle(bx, by, (int)bossFx.TelegraphCoreRadius, bossFx.TelegraphCoreColor, true);

                         if (fsm.CurrentSkillName == "DiveRam")
                         {
                             int tx = (int)fsm.TargetX;
                             int ty = (int)fsm.TargetY;
                             int lockRadius = (int)(45.0f - progress * 15.0f);
                             GameEngine.DrawCircle(tx, ty, lockRadius, ColorPalette.RedBright, false);
                             GameEngine.DrawCircle(tx, ty, lockRadius - 2, ColorPalette.Gold, false);
                         }
                     }
                     else if (fsm.CurrentState == Boss1StateKind.Casting && fsm.CurrentSkillName == "DiveRam")
                     {
                         GameEngine.DrawCircle(bx, by, 90, ColorPalette.RedBright, false);
                         GameEngine.DrawCircle(bx, by, 85, ColorPalette.Gold, false);
                         GameEngine.DrawCircle(bx, by, 60, ColorPalette.RedTelegraphCore, true);
                     }
                     else if (fsm.CurrentState == Boss1StateKind.PhaseTransition || fsm.IsInvulnerable)
                     {
                         int shieldRadius = (int)(bossFx.ShieldBaseRadius + MathF.Sin(fsm.StateTimer * bossFx.ShieldPulseSpeed) * bossFx.ShieldPulseAmp);
                         GameEngine.DrawCircle(bx, by, shieldRadius, bossFx.ShieldOuterColor, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 3, bossFx.ShieldInnerColor, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 8, bossFx.ShieldFillColor, true);
                     }
                     else if (fsm.CurrentState == Boss1StateKind.Recovering)
                     {
                         GameEngine.DrawCircle(bx, by, (int)bossFx.RecoveringAuraRadius, bossFx.RecoveringAuraColor, true);
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

                 boss2Query.Each((ref Transform trans, ref Health bHealth, ref Boss2Stats stats, ref Boss2StateMachine fsm, ref Sprite sprite) =>
                 {
                     int bx = (int)trans.X;
                     int by = (int)trans.Y;

                     if (fsm.IsTelegraphing)
                     {
                         Boss2Config b2Cfg = ConfigManager.Boss2;
                         float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                         int ringRadius = (int)(bossFx.TelegraphStartRadius - progress * bossFx.TelegraphEndRadiusDelta);

                         switch (fsm.CurrentState)
                         {
                             case Boss2StateKind.TelegraphGroundStomp:
                                 {
                                     float groundY = trans.Y + b2Cfg.CollisionHeight / 2.0f - 10.0f;
                                     int leftFx = (int)(bx - 35.0f);
                                     int rightFx = (int)(bx + 35.0f);
                                     int fy = (int)groundY;

                                     GameEngine.DrawCircle(leftFx, fy, ringRadius, ColorPalette.Amber, false);
                                     GameEngine.DrawCircle(leftFx, fy, ringRadius - 2, ColorPalette.RedOrange, false);
                                     GameEngine.DrawCircle(leftFx, fy, (int)bossFx.TelegraphCoreRadius, ColorPalette.OrangeBright, true);

                                     GameEngine.DrawCircle(rightFx, fy, ringRadius, ColorPalette.Amber, false);
                                     GameEngine.DrawCircle(rightFx, fy, ringRadius - 2, ColorPalette.RedOrange, false);
                                     GameEngine.DrawCircle(rightFx, fy, (int)bossFx.TelegraphCoreRadius, ColorPalette.OrangeBright, true);
                                 }
                                 break;

                             case Boss2StateKind.TelegraphLeapSmash:
                                 {
                                     int tx = (int)fsm.TargetX;
                                     int ty = (int)fsm.TargetY;
                                     int lockRadius = (int)(50.0f - progress * 15.0f);
                                     GameEngine.DrawCircle(tx, ty, lockRadius, ColorPalette.RedBright, false);
                                     GameEngine.DrawCircle(tx, ty, lockRadius - 2, ColorPalette.Gold, false);
                                     GameEngine.DrawCircle(tx, ty, 8, ColorPalette.OrangeBright, true);

                                     GameEngine.DrawCircle(bx, by, ringRadius, ColorPalette.RedOrange, false);
                                 }
                                 break;

                             case Boss2StateKind.TelegraphEarthquakeRoar:
                                 {
                                     GameEngine.DrawCircle(bx, by, ringRadius + 15, ColorPalette.Amber, false);
                                     GameEngine.DrawCircle(bx, by, ringRadius, ColorPalette.RedOrange, false);
                                     GameEngine.DrawCircle(bx, by, (int)bossFx.TelegraphCoreRadius + 5, ColorPalette.OrangeBright, true);
                                 }
                                 break;

                             default:
                                 {
                                     float forwardDir = stats.FacingRight ? 1.0f : -1.0f;
                                     int targetFx = (int)(bx + forwardDir * b2Cfg.SlashSpawnOffsetX);
                                     int targetFy = (int)(by + b2Cfg.SlashSpawnOffsetY);

                                     GameEngine.DrawCircle(targetFx, targetFy, ringRadius, ColorPalette.RedOrange, false);
                                     GameEngine.DrawCircle(targetFx, targetFy, ringRadius - 2, ColorPalette.Amber, false);
                                     GameEngine.DrawCircle(targetFx, targetFy, (int)bossFx.TelegraphCoreRadius, ColorPalette.OrangeBright, true);
                                 }
                                 break;
                         }
                     }
                     else if (fsm.CurrentState == Boss2StateKind.PhaseTransition || fsm.IsInvulnerable)
                     {
                         int shieldRadius = (int)(bossFx.ShieldBaseRadius + MathF.Sin(fsm.StateTimer * bossFx.ShieldPulseSpeed) * bossFx.ShieldPulseAmp);
                         GameEngine.DrawCircle(bx, by, shieldRadius, ColorPalette.RedCrimson, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 3, ColorPalette.Amber, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 8, ColorPalette.RedTelegraphCore, true);
                     }
                     else if (fsm.CurrentState == Boss2StateKind.SkillRecovery)
                     {
                         GameEngine.DrawCircle(bx, by, (int)bossFx.RecoveringAuraRadius, ColorPalette.CyanRecovering, true);
                     }
                     else if (fsm.CurrentState == Boss2StateKind.WallCrashStun)
                     {
                         float angle = fsm.StateTimer * 6.0f;
                         for (int k = 0; k < 3; k++)
                         {
                             float starAngle = angle + k * (MathF.PI * 2.0f / 3.0f);
                             int sx = (int)(bx + MathF.Cos(starAngle) * 26.0f);
                             int sy = (int)(by - 42.0f + MathF.Sin(starAngle) * 8.0f);
                             GameEngine.DrawCircle(sx, sy, 6, ColorPalette.Gold, true);
                             GameEngine.DrawCircle(sx, sy, 7, ColorPalette.Amber, false);
                         }
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

                     if (health.IsInvincible)
                     {
                         GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 24, ColorPalette.InvincibleShield, false);
                     }
                 });

                 ArenaConfig arenaCfg = ConfigManager.Arena;
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
             });

        return world;
    }
}
