using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// アリーナ足場、Boss (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
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

        Query<Transform, Health, BossStats, BossStateMachine, Sprite> bossQuery =
            world.QueryBuilder<Transform, Health, BossStats, BossStateMachine, Sprite>()
                 .With<BossTag>()
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

        world.System("WorldRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 PlatformRenderConfig platCfg = renderCfg.Platform;
                 BossEffectsRenderConfig bossFx = renderCfg.BossEffects;
                 PlayerRenderConfig playerRenderCfg = renderCfg.Player;
                 BulletRenderConfig bulletCfg = renderCfg.Bullets;

                 platformQuery.Each((ref Transform trans, ref CollisionBox col) =>
                 {
                     int x1 = (int)(trans.X - col.Width / 2.0f);
                     int y1 = (int)(trans.Y - col.Height / 2.0f);
                     int x2 = (int)(trans.X + col.Width / 2.0f);
                     int y2 = (int)(trans.Y + col.Height / 2.0f);

                     GameEngine.DrawBox(x1, y1, x2, y2, platCfg.FillColor, true);
                     GameEngine.DrawBox(x1, y1, x2, y2, platCfg.BorderColor, false);
                     GameEngine.DrawBox(
                         x1 + platCfg.TopHighlightOffset,
                         y1 + platCfg.TopHighlightOffset,
                         x2 - platCfg.TopHighlightOffset,
                         y1 + platCfg.TopHighlightOffset + platCfg.TopHighlightThickness,
                         platCfg.TopHighlightColor,
                         true);
                 });

                 bossQuery.Each((ref Transform trans, ref Health bHealth, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
                 {
                     Color auraColor = stats.Phase switch
                     {
                         1 => bossFx.Phase1AuraColor,
                         2 => bossFx.Phase2AuraColor,
                         3 => bossFx.Phase3AuraColor,
                         _ => GetFallbackAuraColor(stats.Phase, bossFx.Phase1AuraColor)
                     };

                     int bx = (int)trans.X;
                     int by = (int)trans.Y;

                     if (fsm.CurrentState == BossStateKind.Telegraphing)
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
                     else if (fsm.CurrentState == BossStateKind.Casting && fsm.CurrentSkillName == "DiveRam")
                     {
                         GameEngine.DrawCircle(bx, by, 90, ColorPalette.RedBright, false);
                         GameEngine.DrawCircle(bx, by, 85, ColorPalette.Gold, false);
                         GameEngine.DrawCircle(bx, by, 60, ColorPalette.RedTelegraphCore, true);
                     }
                     else if (fsm.CurrentState == BossStateKind.PhaseTransition || fsm.IsInvulnerable)
                     {
                         int shieldRadius = (int)(bossFx.ShieldBaseRadius + MathF.Sin(fsm.StateTimer * bossFx.ShieldPulseSpeed) * bossFx.ShieldPulseAmp);
                         GameEngine.DrawCircle(bx, by, shieldRadius, bossFx.ShieldOuterColor, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 3, bossFx.ShieldInnerColor, false);
                         GameEngine.DrawCircle(bx, by, shieldRadius - 8, bossFx.ShieldFillColor, true);
                     }
                     else if (fsm.CurrentState == BossStateKind.Recovering)
                     {
                         GameEngine.DrawCircle(bx, by, (int)bossFx.RecoveringAuraRadius, bossFx.RecoveringAuraColor, true);
                     }

                     int auraRadius = (int)(bossFx.AuraBaseRadius + MathF.Sin(stats.HoverTimer * bossFx.AuraPulseSpeed) * bossFx.AuraPulseAmp);
                     GameEngine.DrawCircle(bx, by, auraRadius, auraColor, false);
                     GameEngine.DrawCircle(bx, by, auraRadius - 2, auraColor, false);

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
                             bossFx.BossSpriteScale,
                             bossAngle,
                             sprite.GraphHandle,
                             true,
                             false);
                     }

                     if (stats.HitFlashTimer > 0.0f)
                     {
                         GameEngine.DrawCircle(bx, by, (int)bossFx.HitFlashRadius, bossFx.HitFlashColor, true);
                     }
                 });

                 // 3. プレイヤー自機描画
                 playerQuery.Each((ref Transform trans, ref Health health, ref PlayerState playerState, ref Sprite sprite) =>
                 {
                     if (!health.IsAlive || !sprite.IsVisible || sprite.GraphHandle < 0)
                     {
                         return;
                     }

                     // 無敵時間点滅演出（i-frames flickering）
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

                 // 4. プレイヤー銃弾描画（SIMD 原生批量レンダリング）
                 playerBulletQuery.Iter((Iter it, Field<Transform> trans, Field<Velocity> vel) =>
                 {
                     int count = it.Count();
                     if (count <= 0)
                     {
                         return;
                     }

                     unsafe
                     {
                         Transform* pTrans = (Transform*)trans.Data;
                         Velocity* pVel = (Velocity*)vel.Data;

                         GameEngine.BatchDrawPlayerBullets(
                             pTrans,
                             pVel,
                             count,
                             (int)bulletCfg.PlayerBulletOuterRadius,
                             bulletCfg.PlayerBulletOuterColor,
                             (int)bulletCfg.PlayerBulletInnerRadius,
                             bulletCfg.PlayerBulletInnerColor,
                             bulletCfg.PlayerBulletTrailHalfWidth,
                             bulletCfg.PlayerBulletTrailHalfHeight * 2,
                             bulletCfg.PlayerBulletTrailColor,
                             clipMinX,
                             clipMaxX,
                             clipMinY,
                             clipMaxY);
                     }
                 });

                 // 5. Boss 弾幕描画（SIMD 原生批量レンダリング）
                 bossBulletQuery.Iter((Iter it, Field<Transform> trans, Field<Bullet> bullet) =>
                 {
                     int count = it.Count();
                     if (count <= 0)
                     {
                         return;
                     }

                     unsafe
                     {
                         Transform* pTrans = (Transform*)trans.Data;
                         Bullet b = bullet[0];

                         GameEngine.BatchDrawBossBullets(
                             pTrans,
                             count,
                             (int)b.Radius,
                             b.FillColor,
                             b.OutlineColor,
                             bulletCfg.BossBulletOutlinePadding,
                             bulletCfg.BossBulletCoreRadiusRatio,
                             bulletCfg.BossBulletCoreMinRadius,
                             bulletCfg.BossBulletCoreColor,
                             clipMinX,
                             clipMaxX,
                             clipMinY,
                             clipMaxY);
                     }
                 });
             });

        return world;
    }

    private static Color GetFallbackAuraColor(int phase, Color defaultColor)
    {
        Console.WriteLine($"[WorldRender 警告] 未知の Boss Phase 番号です: {phase} (有効値: 1, 2, 3)");
        return defaultColor;
    }
}
