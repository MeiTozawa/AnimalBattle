using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using AnimalBattle.Scenes;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     アリーナ足場・タイルマップ、Boss 1 / Boss 2 (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
///     プレイヤー自機 (スプライト・無敵点滅)、および 8 方向プレイヤー弾・Boss 弾幕・近接斬撃エフェクトを描画する世界描画システム。
///     領域モデル（<see cref="Player" /> / <see cref="BossCharacter" />）から直接キャラクターを描画し、
///     海量弾幕・パーティクルエフェクトのみを Flecs ECS 高速クエリで一括描画します。
/// </summary>
/// <remarks>
///     <see cref="WorldRenderSystem" /> の新しいインスタンスを初期化し、弾幕・エフェクト用のクエリを構築します。
/// </remarks>
/// <param name="world">描画対象の Flecs.NET ワールド。</param>
public sealed class WorldRenderSystem : IDisposable {
    private readonly Ecs.IterFieldCallback<Transform, Bullet> _drawBossBulletsCallback;

    // 毎フレームの Iter コールバックでのクロージャ割り当てを完全に排除するための事前確保デリゲート
    private readonly Ecs.IterFieldCallback<Transform, Velocity> _drawPlayerBulletsCallback;
    private Color _bCoreColor;
    private int _bCoreMinRadius;
    private float _bCoreRadiusRatio;
    private Query<Transform, Bullet> _bossBulletQuery;

    private int _bOutlinePadding;
    private int _bulletClipMaxX;
    private int _bulletClipMaxY;

    // 描画コンテキストパラメータキャッシュ（ゼロ GC 描画用）
    private int _bulletClipMinX;
    private int _bulletClipMinY;
    private Query<Transform, Sprite, HitFlashEffect> _hitFlashQuery;
    private bool _isDisposed;
    private Query<Transform, Bullet, FadeEffect> _particleEffectQuery;
    private Color _pInnerColor;
    private int _pInnerRadius;
    private Query<Transform, Velocity> _playerBulletQuery;
    private Color _pOuterColor;

    private int _pOuterRadius;
    private int _pTrailOffset;
    private Query<Transform, RingEffect, FadeEffect> _ringEffectQuery;
    private Query<Transform, Sprite, FadeEffect> _spriteEffectQuery;

    /// <summary>
    ///     <see cref="WorldRenderSystem" /> の新しいインスタンスを初期化し、弾幕・エフェクト用のクエリと不変デリゲートを構築します。
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
        _spriteEffectQuery = world.QueryBuilder<Transform, Sprite, FadeEffect>()
           .With<EffectTag>()
           .Build();
        _ringEffectQuery = world.QueryBuilder<Transform, RingEffect, FadeEffect>()
           .With<EffectTag>()
           .Build();
        _particleEffectQuery = world.QueryBuilder<Transform, Bullet, FadeEffect>()
           .With<EffectTag>()
           .Build();
        _hitFlashQuery = world.QueryBuilder<Transform, Sprite, HitFlashEffect>()
           .Build();

        _drawPlayerBulletsCallback = DrawPlayerBulletsBatch;
        _drawBossBulletsCallback = DrawBossBulletsBatch;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _playerBulletQuery.Dispose();
            _bossBulletQuery.Dispose();
            _spriteEffectQuery.Dispose();
            _ringEffectQuery.Dispose();
            _particleEffectQuery.Dispose();
            _hitFlashQuery.Dispose();
            _isDisposed = true;
        }
    }

    /// <summary>
    ///     世界の全ゲームオブジェクト（マップ、足場、キャラクター、弾幕、エフェクト）を描画します。
    ///     統一レンダリングパイプラインとして、直接シーン状態または ECS クエリから描画対象を解決します。
    /// </summary>
    /// <param name="tileMap">現在のアリーナタイルマップ。</param>
    /// <param name="platformQuery">プラットフォーム足場検索クエリ。</param>
    public void Render(TileMap? tileMap, Query<Transform, CollisionBox> platformQuery)
    {
        Render(tileMap, platformQuery, BattleScene.Current?.Player, BattleScene.Current?.Boss);
    }

    /// <summary>
    ///     世界の全ゲームオブジェクト（マップ、Boss、自機、弾幕、エフェクト）を描画します。
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
        var renderCfg = ConfigManager.Render;
        var bossFx = renderCfg.BossEffects;
        var bulletCfg = renderCfg.Bullets;
        var arenaCfg = ConfigManager.Arena;

        var playerX = player?.X ?? ConfigManager.Player.SpawnX;
        var playerY = player?.Y ?? ConfigManager.Player.SpawnY;

        DrawPlatforms(tileMap, platformQuery, renderCfg.Viewport);
        DrawBoss(boss, bossFx, playerX, playerY);
        DrawPlayer(player);
        DrawHitFlashEffects(_hitFlashQuery);

        UpdateBulletRenderContext(bulletCfg, arenaCfg);
        _playerBulletQuery.Iter(_drawPlayerBulletsCallback);
        _bossBulletQuery.Iter(_drawBossBulletsCallback);

        DrawSpriteEffects(_spriteEffectQuery);
        DrawRingEffects(_ringEffectQuery);
        DrawParticleEffects(_particleEffectQuery);
    }

    private static void DrawPlatforms(TileMap? tileMap, Query<Transform, CollisionBox> platformQuery, ViewportRenderConfig viewport)
    {
        if (tileMap != null)
            tileMap.DrawCulled(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        else
            platformQuery.Each((ref trans, ref col) =>
            {
                var x1 = (int)(trans.X - col.Width / 2.0f);
                var y1 = (int)(trans.Y - col.Height / 2.0f);
                var x2 = (int)(trans.X + col.Width / 2.0f);
                var y2 = (int)(trans.Y + col.Height / 2.0f);

                GameEngine.DrawBox(x1, y1, x2, y2, ColorPalette.DarkBg, true);
                GameEngine.DrawBox(x1, y1, x2, y2, ColorPalette.Cyan, false);
            });
    }

    private static void DrawBoss(BossCharacter? boss, BossEffectsRenderConfig bossFx, float playerX, float playerY)
    {
        if (boss == null || !boss.IsAlive || !boss.Sprite.IsVisible || boss.Sprite.GraphHandle < 0) return;

        var bx = (int)boss.X;
        var by = (int)boss.Y;

        if (boss.BossType == BossType.Boss1)
        {
            if (boss.CurrentState == BossState.Boss1_Telegraph)
            {
                var progress = Math.Clamp(boss.StateTimer / Math.Max(0.01f, boss.StateDuration), 0.0f, 1.0f);
                DrawBoss1Telegraph(boss.X, boss.Y, boss.CurrentSkill, boss.TargetX, boss.TargetY, playerX, playerY,
                    progress, boss.StateTimer);
            }

            var bossAngle = MathF.Sin(boss.HoverTimer * bossFx.BossHoverAngleSpeed) * bossFx.BossHoverAngleAmp;
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
                DrawBossVulnerableEffects(
                    bx,
                    by,
                    boss.Sprite.SrcRect,
                    boss.ScaleX,
                    bossAngle,
                    boss.Sprite.GraphHandle,
                    false,
                    boss.StateTimer,
                    bossFx);
        }
        else
        {
            if (boss.CurrentState == BossState.Boss2_Telegraph)
            {
                var progress = Math.Clamp(boss.StateTimer / Math.Max(0.01f, boss.StateDuration), 0.0f, 1.0f);
                DrawBoss2Telegraph(boss.X, boss.Y, boss.CurrentSkill, boss.TargetX, boss.TargetY, playerX, playerY,
                    boss.FacingRight, progress);
            }

            var flipX = !boss.FacingRight;
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
                DrawBossVulnerableEffects(
                    bx,
                    by,
                    boss.Sprite.SrcRect,
                    boss.ScaleX,
                    0.0,
                    boss.Sprite.GraphHandle,
                    flipX,
                    boss.StateTimer,
                    bossFx);
        }
    }

    private static void DrawBossVulnerableEffects(
        int bx,
        int by,
        in Rect srcRect,
        float scaleX,
        double angle,
        int graphHandle,
        bool flipX,
        float stateTimer,
        BossEffectsRenderConfig bossFx)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(stateTimer * 10.0f);
        var vulnAlpha = (int)(130 + 110 * pulse);
        GameEngine.SetDrawBlendMode(BlendMode.Add, vulnAlpha);
        GameEngine.DrawRectRotaGraph(
            bx,
            by,
            srcRect.X1,
            srcRect.Y1,
            srcRect.Width,
            srcRect.Height,
            scaleX * 1.04f,
            angle,
            graphHandle,
            true,
            flipX);
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);

        var bobbing = MathF.Sin(stateTimer * 8.0f) * 4.0f;
        var textWidth = bossFx.ChanceMarkerText.Length * 7;
        var textX = bx - textWidth / 2;
        var textY = (int)(by - bossFx.ChanceMarkerOffsetY + bobbing);
        GameEngine.DrawString(textX, textY, bossFx.ChanceMarkerText, bossFx.ChanceMarkerColor);
    }

    private static void DrawBoss1Telegraph(
        float bx,
        float by,
        BossSkill skill,
        float targetX,
        float targetY,
        float playerX,
        float playerY,
        float progress,
        float stateTimer)
    {
        var patternCfg = ConfigManager.Pattern;
        var finalTargetX = targetX != 0.0f || targetY != 0.0f ? targetX : playerX;
        var finalTargetY = targetX != 0.0f || targetY != 0.0f ? targetY : playerY;

        switch (skill)
        {
            case BossSkill.Aimed3Shot:
                TelegraphRenderHelper.DrawAimingLasers(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    patternCfg.Aimed3Shot.SpreadAngle,
                    patternCfg.Aimed3Shot.BulletCount,
                    progress,
                    patternCfg.Aimed3Shot.TelegraphColor);
            break;

            case BossSkill.FastSniper:
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

            case BossSkill.DiveRam:
                TelegraphRenderHelper.DrawDiveRamTelegraph(
                    bx,
                    by,
                    finalTargetX,
                    finalTargetY,
                    patternCfg.DiveRam.ImpactRadius,
                    progress,
                    patternCfg.DiveRam.TelegraphColor);
            break;

            case BossSkill.RingBurst:
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    bx,
                    by,
                    patternCfg.RingBurst.BulletCount,
                    0.0f,
                    75.0f,
                    progress,
                    patternCfg.RingBurst.TelegraphColor);
            break;

            case BossSkill.DoubleSpiral:
                var spiralAngle = stateTimer * 4.0f;
                TelegraphRenderHelper.DrawRadialThreatSpikes(
                    bx,
                    by,
                    2,
                    spiralAngle,
                    90.0f,
                    progress,
                    patternCfg.DoubleSpiral.TelegraphColor);
            break;

            case BossSkill.RainDrop:
                TelegraphRenderHelper.DrawSkyfallColumns(
                    patternCfg.RainDrop.MinX,
                    patternCfg.RainDrop.MaxX,
                    6,
                    650.0f,
                    progress,
                    patternCfg.RainDrop.TelegraphColor,
                    "▼ RAIN ▼");
            break;

            case BossSkill.CrossChaos:
                var crossAngle = stateTimer * 2.5f;
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
                    patternCfg.Aimed3Shot.TelegraphColor);
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
        var b2Cfg = ConfigManager.Boss2;
        var patternCfg = ConfigManager.Pattern;
        var finalTargetX = targetX != 0.0f || targetY != 0.0f ? targetX : playerX;
        var finalTargetY = targetX != 0.0f || targetY != 0.0f ? targetY : playerY;

        switch (skill)
        {
            case BossSkill.GroundStomp:
                var groundY = by + b2Cfg.CollisionHeight / 2.0f;
                TelegraphRenderHelper.DrawGroundShockwaveRails((int)bx, groundY, progress,
                    patternCfg.GroundStomp.TelegraphColor);
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
        if (player == null || !player.IsAlive || !player.Sprite.IsVisible || player.Sprite.GraphHandle < 0) return;

        var isFlashing = player.IsFlashing;
        var isInvincibleBlinking = !isFlashing && player.InvincibleTimer > 0.0f &&
            (int)(player.InvincibleTimer * 30.0f) % 2 != 0;

        if (!isInvincibleBlinking)
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

    private void UpdateBulletRenderContext(BulletRenderConfig bulletCfg, ArenaConfig arenaCfg)
    {
        _bulletClipMinX = (int)arenaCfg.BulletDespawnMinX - bulletCfg.ViewportCullingMarginX;
        _bulletClipMaxX = (int)arenaCfg.BulletDespawnMaxX + bulletCfg.ViewportCullingMarginX;
        _bulletClipMinY = (int)arenaCfg.BulletDespawnMinY - bulletCfg.ViewportCullingMarginY;
        _bulletClipMaxY = (int)arenaCfg.BulletDespawnMaxY + bulletCfg.ViewportCullingMarginY;

        _pOuterRadius = (int)bulletCfg.PlayerBulletOuterRadius;
        _pOuterColor = bulletCfg.PlayerBulletOuterColor;
        _pInnerRadius = (int)bulletCfg.PlayerBulletInnerRadius;
        _pInnerColor = bulletCfg.PlayerBulletInnerColor;
        _pTrailOffset = bulletCfg.PlayerBulletTrailHalfWidth;

        _bOutlinePadding = bulletCfg.BossBulletOutlinePadding;
        _bCoreRadiusRatio = bulletCfg.BossBulletCoreRadiusRatio;
        _bCoreMinRadius = bulletCfg.BossBulletCoreMinRadius;
        _bCoreColor = bulletCfg.BossBulletCoreColor;
    }

    private void DrawPlayerBulletsBatch(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        for (var i = 0; i < count; ++i)
        {
            var px = (int)trans[i].X;
            var py = (int)trans[i].Y;
            if (px < _bulletClipMinX || px > _bulletClipMaxX || py < _bulletClipMinY || py > _bulletClipMaxY) continue;

            var vx = vel[i].Vx;
            var vy = vel[i].Vy;
            var speedSq = vx * vx + vy * vy;
            if (speedSq > 1.0f)
            {
                var invSpeed = 1.0f / MathF.Sqrt(speedSq);
                var dirX = vx * invSpeed;
                var dirY = vy * invSpeed;

                var trailX = (int)(px - dirX * _pTrailOffset);
                var trailY = (int)(py - dirY * _pTrailOffset);
                GameEngine.DrawCircle(trailX, trailY, _pInnerRadius, _pOuterColor, true);
            }

            GameEngine.DrawCircle(px, py, _pOuterRadius, _pOuterColor, true);
            GameEngine.DrawCircle(px, py, _pInnerRadius, _pInnerColor, true);
        }
    }

    private void DrawBossBulletsBatch(Iter it, Field<Transform> trans, Field<Bullet> bullet)
    {
        var count = it.Count();
        for (var i = 0; i < count; ++i)
        {
            var bx = (int)trans[i].X;
            var by = (int)trans[i].Y;
            if (bx < _bulletClipMinX || bx > _bulletClipMaxX || by < _bulletClipMinY || by > _bulletClipMaxY) continue;

            var b = bullet[i];
            var radius = (int)b.Radius;
            var outlineRadius = radius + _bOutlinePadding;
            var coreRadius = Math.Max(_bCoreMinRadius, (int)(radius * _bCoreRadiusRatio));

            var e = it.Entity(i);
            if (e.Has<BulletModifier>())
            {
                ref readonly var mod = ref e.Get<BulletModifier>();
                DrawSpecialBossBullet(bx, by, in b, in mod, radius, outlineRadius, coreRadius, _bOutlinePadding,
                    _bCoreRadiusRatio, _bCoreColor);
            }
            else
            {
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, _bCoreColor, true);
            }
        }
    }

    private static void DrawSpecialBossBullet(
        int bx,
        int by,
        in Bullet b,
        in BulletModifier mod,
        int radius,
        int outlineRadius,
        int coreRadius,
        int outlinePadding,
        float coreRadiusRatio,
        Color coreColor)
    {
        switch (mod.Type)
        {
            case BulletType.Magenta: {
                // 品紅色：周期的に明滅・脈動する外周半透明オーラ輪
                var pulseAura = radius * (1.35f + 0.15f * MathF.Sin(mod.ElapsedTimer * 6.0f));
                var auraColor = Color.FromArgb(65, b.FillColor.R, b.FillColor.G, b.FillColor.B);
                GameEngine.DrawCircle(bx, by, (int)pulseAura, auraColor, false);
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Yellow: {
                // 黄色：主円と衛星小円の二重円構成（自転・公転）
                var orbitDist = radius * 1.5f;
                var angle = mod.ElapsedTimer * 6.0f;
                var satX = (int)(bx + MathF.Cos(angle) * orbitDist);
                var satY = (int)(by + MathF.Sin(angle) * orbitDist);
                var satRadius = Math.Max(3, (int)(radius * 0.55f));
                var satOutline = satRadius + outlinePadding;
                var satCore = Math.Max(2, (int)(satRadius * coreRadiusRatio));

                // 衛星小円の描画
                GameEngine.DrawCircle(satX, satY, satOutline, b.OutlineColor, true);
                GameEngine.DrawCircle(satX, satY, satRadius, b.FillColor, true);
                GameEngine.DrawCircle(satX, satY, satCore, coreColor, true);

                // 主円の描画
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Cyan: {
                // 青色：電光シアンの輪郭とエフェクトリング
                GameEngine.DrawCircle(bx, by, outlineRadius + 2, Color.FromArgb(50, 0, 240, 255), false);
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }

            case BulletType.Black: {
                // 黑色：全 3 種特効統合（呼吸拡縮オーラ + 自転二重円 + 虚空暗黒芯）
                var pulseAura = radius * (1.4f + 0.2f * MathF.Sin(mod.ElapsedTimer * 6.0f));
                GameEngine.DrawCircle(bx, by, (int)pulseAura, Color.FromArgb(90, 220, 20, 80), false);

                // 衛星小円（黒核・赤枠）
                var orbitDist = radius * 1.55f;
                var angle = mod.ElapsedTimer * 6.0f;
                var satX = (int)(bx + MathF.Cos(angle) * orbitDist);
                var satY = (int)(by + MathF.Sin(angle) * orbitDist);
                var satRadius = Math.Max(3, (int)(radius * 0.55f));
                var satOutline = satRadius + outlinePadding;

                GameEngine.DrawCircle(satX, satY, satOutline, b.OutlineColor, true);
                GameEngine.DrawCircle(satX, satY, satRadius, b.FillColor, true);

                // 主円（深黒コア + ネオン発光輪郭 + 微小ホワイト核）
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, Math.Max(2, coreRadius / 2), Color.FromArgb(255, 255, 180, 200), true);
                break;
            }

            default: {
                GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
                GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
                GameEngine.DrawCircle(bx, by, coreRadius, coreColor, true);
                break;
            }
        }
    }

    private static void DrawHitFlashEffects(Query<Transform, Sprite, HitFlashEffect> hitFlashQuery)
    {
        hitFlashQuery.Each((ref trans, ref sprite, ref flash) =>
        {
            if (!sprite.IsVisible || sprite.GraphHandle < 0) return;

            var alpha = (int)(255 * flash.Intensity);
            if (alpha <= 0) return;

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

    private static void DrawSpriteEffects(Query<Transform, Sprite, FadeEffect> spriteEffectQuery)
    {
        spriteEffectQuery.Each((ref trans, ref sprite, ref fade) =>
        {
            var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
            if (alpha <= 0 || !sprite.IsVisible || sprite.GraphHandle < 0) return;

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
        });
    }

    private static void DrawRingEffects(Query<Transform, RingEffect, FadeEffect> ringEffectQuery)
    {
        ringEffectQuery.Each((ref trans, ref ring, ref fade) =>
        {
            var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
            if (alpha <= 0) return;

            var progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
            var currentRadius = ring.StartRadius + (ring.EndRadius - ring.StartRadius) * progress;

            GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
            GameEngine.DrawCircle((int)trans.X, (int)trans.Y, (int)currentRadius, ring.Color, ring.IsFilled);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        });
    }

    private static void DrawParticleEffects(Query<Transform, Bullet, FadeEffect> particleEffectQuery)
    {
        particleEffectQuery.Each((ref trans, ref b, ref fade) =>
        {
            var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
            if (alpha <= 0) return;

            var px = (int)trans.X;
            var py = (int)trans.Y;
            var radius = (int)b.Radius;

            GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
            GameEngine.DrawCircle(px, py, radius + 1, b.OutlineColor, true);
            GameEngine.DrawCircle(px, py, radius, b.FillColor, true);
            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        });
    }
}