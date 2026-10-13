using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using AnimalBattle.Scenes;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     アリーナ足場・タイルマップ、Boss 1 / Boss 2 (オーラ・無敵シールド・予兆リング・スプライト・被弾フラッシュ・突進残影)、
///     プレイヤー自機 (スプライト・無敵点滅)、および 8 方向プレイヤー弾・Boss 弾幕・近接斬撃エフェクトを描画する世界描画システム。
///     領域モデル（<see cref="Pawn" /> / <see cref="Characters.Boss.BaseBossAgent" />）から直接キャラクターを描画し、
///     海量弾幕・パーティクルエフェクトのみを Flecs ECS 高速クエリで一括描画します。
/// </summary>
/// <remarks>
///     <see cref="WorldRenderSystem" /> の新しいインスタンスを初期化し、弾幕・エフェクト用のクエリを構築します。
/// </remarks>
public sealed class WorldRenderSystem : IDisposable {
    private readonly Ecs.IterFieldCallback<Transform, Bullet> _drawStandardBossBulletsCallback;
    private readonly Ecs.IterFieldCallback<Transform, Bullet, BulletModifier> _drawSpecialBossBulletsCallback;

    // 毎フレームの Iter コールバックでのクロージャ割り当てを完全に排除するための事前確保デリゲート
    private readonly Ecs.IterFieldCallback<Transform, Velocity> _drawPlayerBulletsCallback;
    private Color _bCoreColor;
    private int _bCoreMinRadius;
    private float _bCoreRadiusRatio;
    private Query<Transform, Bullet> _standardBossBulletQuery;
    private Query<Transform, Bullet, BulletModifier> _specialBossBulletQuery;

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
    private float _pTrailOffset1;
    private float _pTrailOffset2;
    private float _pTrailOffset3;
    private int _pTrailRadius1;
    private int _pTrailRadius2;
    private int _pTrailRadius3;
    private Color _pTrailColor1;
    private Color _pTrailColor2;
    private Color _pTrailColor3;
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
        _standardBossBulletQuery = world.QueryBuilder<Transform, Bullet>()
           .With<BossBulletTag>()
           .Without<BulletModifier>()
           .Build();
        _specialBossBulletQuery = world.QueryBuilder<Transform, Bullet, BulletModifier>()
           .With<BossBulletTag>()
           .Build();
        _spriteEffectQuery = world.QueryBuilder<Transform, Sprite, FadeEffect>()
           .With<VfxTag>()
           .Build();
        _ringEffectQuery = world.QueryBuilder<Transform, RingEffect, FadeEffect>()
           .With<VfxTag>()
           .Build();
        _particleEffectQuery = world.QueryBuilder<Transform, Bullet, FadeEffect>()
           .With<VfxTag>()
           .Build();
        _hitFlashQuery = world.QueryBuilder<Transform, Sprite, HitFlashEffect>()
           .Build();

        _drawPlayerBulletsCallback = DrawPlayerBulletsBatch;
        _drawStandardBossBulletsCallback = DrawStandardBossBulletsBatch;
        _drawSpecialBossBulletsCallback = DrawSpecialBossBulletsBatch;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _playerBulletQuery.Dispose();
            _standardBossBulletQuery.Dispose();
            _specialBossBulletQuery.Dispose();
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
        Render(tileMap, platformQuery, BattleScene.Current.Player, BattleScene.Current.Boss);
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
        Pawn? player,
        BaseBossAgent? boss)
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
        _standardBossBulletQuery.Iter(_drawStandardBossBulletsCallback);
        _specialBossBulletQuery.Iter(_drawSpecialBossBulletsCallback);

        DrawSpriteEffects(_spriteEffectQuery);
        DrawRingEffects(_ringEffectQuery);
        DrawParticleEffects(_particleEffectQuery);
    }

    private static void DrawPlatforms(TileMap? tileMap, Query<Transform, CollisionBox> platformQuery,
        ViewportRenderConfig viewport)
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

    private static void DrawBoss(BaseBossAgent? boss, BossEffectsRenderConfig bossFx, float playerX, float playerY)
    {
        if (boss == null || !boss.IsAlive || !boss.Sprite.IsVisible || boss.Sprite.GraphHandle < 0) return;

        var jitterX = 0.0f;
        var jitterY = 0.0f;
        if (boss.Entity.Has<SpriteJitter>())
        {
            ref readonly var jitter = ref boss.Entity.Get<SpriteJitter>();
            jitterX = jitter.CurrentOffsetX;
            jitterY = jitter.CurrentOffsetY;
        }

        var bx = (int)(boss.X + jitterX);
        var by = (int)(boss.Y + jitterY);

        if (boss.CurrentState.IsTelegraph)
        {
            var progress = Math.Clamp(boss.StateTimer / Math.Max(0.01f, boss.StateDuration), 0.0f, 1.0f);
            DrawBossTelegraph(boss, playerX, playerY, progress);
        }

        var flipX = !boss.FacingRight;
        var rotation = boss.Rotation;

        GameEngine.DrawRectRotaGraph(
            bx,
            by,
            boss.Sprite.SrcRect.X1,
            boss.Sprite.SrcRect.Y1,
            boss.Sprite.SrcRect.Width,
            boss.Sprite.SrcRect.Height,
            boss.ScaleX,
            rotation,
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
                rotation,
                boss.Sprite.GraphHandle,
                flipX,
                boss.StateTimer,
                bossFx);
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

    private static (float targetX, float targetY) ResolveTargetPosition(float targetX, float targetY, float playerX, float playerY)
    {
        return (
            targetX != 0.0f || targetY != 0.0f ? targetX : playerX,
            targetX != 0.0f || targetY != 0.0f ? targetY : playerY
        );
    }

    private static void DrawBossTelegraph(
        BaseBossAgent boss,
        float playerX,
        float playerY,
        float progress)
    {
        var currentAbility = boss.AbilitySystem.CurrentAbility;
        if (currentAbility == null) return;

        var (finalTargetX, finalTargetY) = ResolveTargetPosition(boss.TargetX, boss.TargetY, playerX, playerY);
        AbilityTelegraphContext ctx = new(boss.X, boss.Y, finalTargetX, finalTargetY, boss.FacingRight, progress, boss.StateTimer);
        currentAbility.DrawTelegraph(in ctx);
    }

    private static void DrawPlayer(Pawn? player)
    {
        if (player == null || !player.IsAlive || !player.Sprite.IsVisible || player.Sprite.GraphHandle < 0) return;

        var isFlashing = player.IsFlashing;
        var invTimer = player.Entity.Get<Health>().InvincibleTimer;
        var isInvincibleBlinking = !isFlashing && invTimer > 0.0f &&
            (int)(invTimer * 30.0f) % 2 != 0;

        var jitterX = 0.0f;
        var jitterY = 0.0f;
        if (player.Entity.Has<SpriteJitter>())
        {
            ref readonly var jitter = ref player.Entity.Get<SpriteJitter>();
            jitterX = jitter.CurrentOffsetX;
            jitterY = jitter.CurrentOffsetY;
        }

        var finalScaleX = player.ScaleX;
        var finalScaleY = player.ScaleY;
        var springOffsetY = 0.0f;

        if (player is PlayerPawn playerPawn)
        {
            ref readonly var spring = ref playerPawn.SpringScale;
            finalScaleX *= spring.CurrentX;
            finalScaleY *= spring.CurrentY;

            if (playerPawn.IsGrounded)
            {
                // 接地時は脚底を接触線に固定し、頭部・胴体のみを下方に押し潰し復元
                var halfH = playerPawn.Sprite.SrcRect.Height * playerPawn.ScaleY * 0.5f;
                springOffsetY = halfH * (1.0f - spring.CurrentY);
            }
        }

        if (!isInvincibleBlinking)
            GameEngine.DrawRectRotaGraph(
                (int)(player.X + jitterX),
                (int)(player.Y + jitterY + springOffsetY),
                player.Sprite.SrcRect.X1,
                player.Sprite.SrcRect.Y1,
                player.Sprite.SrcRect.Width,
                player.Sprite.SrcRect.Height,
                finalScaleX,
                finalScaleY,
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

        // トレイル基準色の抽出（未指定時は外層カラーを採用）
        var trailBase = bulletCfg.PlayerBulletTrailColor.A > 0 ? bulletCfg.PlayerBulletTrailColor : _pOuterColor;
        var tr = trailBase.R;
        var tg = trailBase.G;
        var tb = trailBase.B;

        // 3 段階アルファ減衰カラーの事前計算
        _pTrailColor1 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha1, tr, tg, tb);
        _pTrailColor2 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha2, tr, tg, tb);
        _pTrailColor3 = Color.FromArgb(bulletCfg.PlayerBulletTrailAlpha3, tr, tg, tb);

        // 外層半径に基づく各階層ピクセル半径の事前計算
        _pTrailRadius1 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio1));
        _pTrailRadius2 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio2));
        _pTrailRadius3 = Math.Max(1, (int)MathF.Round(_pOuterRadius * bulletCfg.PlayerBulletTrailRadiusRatio3));

        // 速度逆方向オフセット距離（基準幅 × 比率）の事前計算
        _pTrailOffset1 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio1;
        _pTrailOffset2 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio2;
        _pTrailOffset3 = _pTrailOffset * bulletCfg.PlayerBulletTrailOffsetRatio3;

        _bOutlinePadding = bulletCfg.BossBulletOutlinePadding;
        _bCoreRadiusRatio = bulletCfg.BossBulletCoreRadiusRatio;
        _bCoreMinRadius = bulletCfg.BossBulletCoreMinRadius;
        _bCoreColor = bulletCfg.BossBulletCoreColor;
    }

    /// <summary>
    ///     自機弾を描画します。GameEngine.DrawCircle 経由で Stage3 緊密配置図元ストリーム（CompactCirclePacket、20バイト）へ直接記録されます。
    /// </summary>
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

                // 画家アルゴリズム（奥から手前）に従い、3 段階の彗星トレイルを描画
                // 第3階層：先端（最も遠く、最小、最も透明）
                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset3),
                    (int)(py - dirY * _pTrailOffset3),
                    _pTrailRadius3, _pTrailColor3, true);

                // 第2階層：中段
                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset2),
                    (int)(py - dirY * _pTrailOffset2),
                    _pTrailRadius2, _pTrailColor2, true);

                // 第1階層：弾頭近端（弾頭外枠を覆い、滑らかに接続）
                GameEngine.DrawCircle(
                    (int)(px - dirX * _pTrailOffset1),
                    (int)(py - dirY * _pTrailOffset1),
                    _pTrailRadius1, _pTrailColor1, true);
            }

            // 弾頭本体（外層グロー＋内層白色コア）
            GameEngine.DrawCircle(px, py, _pOuterRadius, _pOuterColor, true);
            GameEngine.DrawCircle(px, py, _pInnerRadius, _pInnerColor, true);
        }
    }

    /// <summary>
    ///     標準ボス弾幕を描画します。20 バイト緊密配置バッファへ高密度記録され、C++ 側でレジスタ展開一括ディスパッチされます。
    /// </summary>
    private void DrawStandardBossBulletsBatch(Iter it, Field<Transform> trans, Field<Bullet> bullet)
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

            GameEngine.DrawCircle(bx, by, outlineRadius, b.OutlineColor, true);
            GameEngine.DrawCircle(bx, by, radius, b.FillColor, true);
            GameEngine.DrawCircle(bx, by, coreRadius, _bCoreColor, true);
        }
    }

    private void DrawSpecialBossBulletsBatch(Iter it, Field<Transform> trans, Field<Bullet> bullet, Field<BulletModifier> modifier)
    {
        var count = it.Count();
        for (var i = 0; i < count; ++i)
        {
            var bx = (int)trans[i].X;
            var by = (int)trans[i].Y;
            if (bx < _bulletClipMinX || bx > _bulletClipMaxX || by < _bulletClipMinY || by > _bulletClipMaxY) continue;

            var b = bullet[i];
            ref readonly var mod = ref modifier[i];
            var radius = (int)b.Radius;
            var outlineRadius = radius + _bOutlinePadding;
            var coreRadius = Math.Max(_bCoreMinRadius, (int)(radius * _bCoreRadiusRatio));

            DrawSpecialBossBullet(bx, by, in b, in mod, radius, outlineRadius, coreRadius, _bOutlinePadding, _bCoreColor);
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