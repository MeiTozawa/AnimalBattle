using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Scenes;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアルの環境オブジェクト（目標物・神殿石門）およびUI（操作ガイド看板・プロンプト枠）の描画を統括する ECS 描画システム群。
/// </summary>
public static class TutorialRenderSystem
{
    /// <summary>
    ///     チュートリアル環境描画システム（ワールド空間・自機の背面レイヤー）を登録します。
    /// </summary>
    public static World AddTutorialEnvironmentRenderSystem(this World world)
    {
        world.System("TutorialEnvironmentRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteTutorialEnvironmentRender);

        return world;
    }

    /// <summary>
    ///     チュートリアルUI描画システム（スクリーン空間・最前面レイヤー）を登録します。
    /// </summary>
    public static World AddTutorialUiRenderSystem(this World world)
    {
        world.System("TutorialUiRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteTutorialUiRender);

        return world;
    }

    /// <summary>
    ///     チュートリアル描画システム群（環境＆UI）を一括登録します。
    /// </summary>
    public static World AddTutorialRenderSystem(this World world)
    {
        return world.AddTutorialEnvironmentRenderSystem()
                    .AddTutorialUiRenderSystem();
    }

    /// <summary>
    ///     チュートリアル環境（目標物・神殿石門・Boss動態）の描画パイプライン（自機より背面に配置）。
    /// </summary>
    public static void ExecuteTutorialEnvironmentRender(Iter it)
    {
        var world = it.World();
        if (world.Get<GameFlow>().State != GameState.Tutorial) return;

        var cfg = world.Get<TutorialConfig>() ?? new TutorialConfig();
        var colors = cfg.Colors;
        var assets = world.Get<IAssetManager>();

        // 1. 目標物（コイン／リング）の描画（ワールド空間）
        using var targetQuery = world.QueryBuilder<TutorialTarget, Transform>().Build();
        targetQuery.Each((Entity _, ref TutorialTarget target, ref Transform tr) =>
        {
            var bob = (int)(MathF.Sin(target.FloatPhase) * cfg.Timing.TargetFloatAmplitude);
            var center = new Vec2i((int)tr.Position.X, (int)tr.Position.Y + bob);
            var r = (int)target.Radius;

            // 外枠発光リングと内側コア
            var targetCfg = cfg.TargetRender;
            GameEngine.DrawCircle(center, r, colors.Gold, false);
            GameEngine.DrawCircle(center, Math.Max(targetCfg.MinInnerRadius, (int)(r * targetCfg.InnerRadiusRatio)), colors.Gold, true);
            GameEngine.DrawCenterString(center, targetCfg.Symbol, colors.White);
        });

        // 2. ボス挑戦神殿石門（BossDoor）の環境描画（基座・虚空・柱・Boss・符文：自機より背面）
        using var doorQuery = world.QueryBuilder<TutorialDoor, Transform>().Build();
        doorQuery.Each((Entity _, ref TutorialDoor door, ref Transform tr) =>
        {
            var doorCenter = new Vec2i((int)tr.Position.X, (int)tr.Position.Y);
            var isSnake = door.TargetBoss == BossType.BossSnake;
            var doorItem = isSnake ? cfg.Doors.Snake : cfg.Doors.Lion;

            DrawShrineDoorEnvironment(
                doorCenter,
                door.TargetBoss,
                doorItem,
                cfg.Doors,
                door.IsPlayerInRange,
                door.AnimTimer,
                colors,
                assets);
        });
    }

    /// <summary>
    ///     チュートリアルUI（操作ガイド・神殿銘板・プロンプト・看板）の描画パイプライン（最前面）。
    /// </summary>
    public static void ExecuteTutorialUiRender(Iter it)
    {
        var world = it.World();
        if (world.Get<GameFlow>().State != GameState.Tutorial) return;

        ref readonly var state = ref world.Get<TutorialState>();
        var cfg = world.Get<TutorialConfig>() ?? new TutorialConfig();
        var colors = cfg.Colors;

        // 1. ボス扉の神殿銘板および接近時 [F] 挑戦開始プロンプト枠（UI最前面）
        using var doorQuery = world.QueryBuilder<TutorialDoor, Transform>().Build();
        doorQuery.Each((Entity _, ref TutorialDoor door, ref Transform tr) =>
        {
            var doorCenter = new Vec2i((int)tr.Position.X, (int)tr.Position.Y);
            var isSnake = door.TargetBoss == BossType.BossSnake;
            var doorItem = isSnake ? cfg.Doors.Snake : cfg.Doors.Lion;

            var halfH = cfg.Doors.Size.Y / 2;
            var gateP1 = new Vec2i(doorCenter.X - cfg.Doors.Size.X / 2, doorCenter.Y - halfH);

            DrawShrinePlaqueAndPrompt(
                doorCenter,
                gateP1,
                cfg.Doors,
                doorItem,
                door.IsPlayerInRange,
                door.AnimTimer,
                cfg.Texts.DoorInteractPrompt,
                colors);
        });

        // 2. 画面上部・中央のフェーズ別背景操作ガイド文字
        DrawPhaseGuidance(state, cfg);

        // 3. 戦闘フェーズ（Combat）における全操作ガイド看板
        if ((uint)state.CurrentPhaseIndex < (uint)cfg.Phases.Length &&
            cfg.Phases[state.CurrentPhaseIndex].Type == TutorialPhaseType.Combat)
        {
            DrawControlGuidanceBoard(cfg);
        }

        // 4. リトライ表示
        if (state.IsRetrying)
        {
            GameEngine.DrawCenterString(cfg.Layout.RetryPos, cfg.Texts.RetryPrompt, colors.Red);
        }
    }

    private static void DrawPhaseGuidance(in TutorialState state, TutorialConfig cfg)
    {
        var centerPos = cfg.Layout.GuidanceTitlePos;
        var subPos = cfg.Layout.GuidanceSubPos;
        var colors = cfg.Colors;

        if ((uint)state.CurrentPhaseIndex < (uint)cfg.Phases.Length)
        {
            var phaseCfg = cfg.Phases[state.CurrentPhaseIndex];
            GameEngine.DrawCenterString(centerPos, phaseCfg.Title, colors.Gold);
            GameEngine.DrawCenterString(subPos, phaseCfg.Subtitle, colors.White);
        }
    }

    private static void DrawControlGuidanceBoard(TutorialConfig cfg)
    {
        var p1 = cfg.Layout.ControlBoardP1;
        var p2 = cfg.Layout.ControlBoardP2;
        var colors = cfg.Colors;
        var texts = cfg.Texts;
        var layout = cfg.Layout;

        GameEngine.DrawBox(p1, p2, colors.PanelBg, true);
        GameEngine.DrawBox(p1, p2, colors.PanelBorder, false);

        var startX = p1.X + layout.ControlBoardPadding.X;
        var y = p1.Y + layout.ControlBoardPadding.Y;

        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardTitle, colors.Cyan);
        y += layout.ControlBoardTitleSpacing;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardMove, colors.White);
        y += layout.ControlBoardLineSpacing;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardJump, colors.White);
        y += layout.ControlBoardLineSpacing;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardDash, colors.White);
        y += layout.ControlBoardAttackSpacing;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardAttack, colors.Gold);
    }

    private static void DrawShrineDoorEnvironment(
        Vec2i center,
        BossType bossType,
        TutorialDoorItemConfig doorItem,
        TutorialDoorsConfig doorsCfg,
        bool inRange,
        float animTimer,
        TutorialColorsConfig colors,
        IAssetManager assets)
    {
        var halfW = doorsCfg.Size.X / 2;
        var halfH = doorsCfg.Size.Y / 2;
        var gateP1 = new Vec2i(center.X - halfW, center.Y - halfH);
        var gateP2 = new Vec2i(center.X + halfW, center.Y + halfH);

        var pillarW = doorsCfg.PillarWidth;
        var lintelH = doorsCfg.LintelHeight;
        var pedestalH = doorsCfg.PedestalHeight;
        var pedestalExtraW = doorsCfg.PedestalExtraWidth;

        // 1. 基座祭壇、虚空門洞、双立石柱の幾何描画
        DrawShrineArchitecture(center, gateP1, gateP2, pillarW, lintelH, pedestalH, pedestalExtraW, doorsCfg, doorItem, inRange, animTimer);

        // 2. 左右石柱への古代刻印符文描画
        DrawShrineRunes(gateP1, gateP2, pillarW, lintelH, pedestalH, doorsCfg, doorItem, inRange, animTimer);

        // 3. Boss 画像および固有動態（蛇：法球旋回、獅子：重砸地面＆衝撃波）
        DrawBossAuraAndMotion(center, bossType, gateP2.Y - pedestalH, doorsCfg, doorItem, inRange, animTimer, colors, assets);
    }

    private static void DrawShrineArchitecture(
        Vec2i center,
        Vec2i gateP1,
        Vec2i gateP2,
        int pillarW,
        int lintelH,
        int pedestalH,
        int pedestalExtraW,
        TutorialDoorsConfig dCfg,
        TutorialDoorItemConfig itemCfg,
        bool inRange,
        float animTimer)
    {
        // 1. 足元基座祭壇（二段石階）
        var base1P1 = new Vec2i(gateP1.X - pedestalExtraW, gateP2.Y - pedestalH / 2);
        var base1P2 = new Vec2i(gateP2.X + pedestalExtraW, gateP2.Y + pedestalH / 2);
        GameEngine.DrawBox(base1P1, base1P2, dCfg.StoneShadow, true);
        GameEngine.DrawBox(base1P1, base1P2, dCfg.StoneHighlight, false);

        var base2P1 = new Vec2i(gateP1.X - pedestalExtraW / 2, gateP2.Y - pedestalH);
        var base2P2 = new Vec2i(gateP2.X + pedestalExtraW / 2, gateP2.Y);
        GameEngine.DrawBox(base2P1, base2P2, dCfg.StoneBase, true);
        GameEngine.DrawBox(base2P1, base2P2, inRange ? itemCfg.RuneColorActive : dCfg.StoneHighlight, false);

        // 2. 虚空門洞（Inner Void）
        var voidP1 = new Vec2i(gateP1.X + pillarW, gateP1.Y + lintelH);
        var voidP2 = new Vec2i(gateP2.X - pillarW, gateP2.Y - pedestalH);
        GameEngine.DrawBox(voidP1, voidP2, dCfg.VoidBg, true);

        // 3. 立体双立石柱
        var lp1 = new Vec2i(gateP1.X, gateP1.Y + lintelH);
        var lp2 = new Vec2i(gateP1.X + pillarW, gateP2.Y - pedestalH);
        GameEngine.DrawBox(lp1, lp2, dCfg.StoneBase, true);
        GameEngine.DrawLine(lp1, new Vec2i(lp1.X, lp2.Y), dCfg.StoneHighlight);
        GameEngine.DrawLine(new Vec2i(lp2.X, lp1.Y), lp2, dCfg.StoneShadow);

        var rp1 = new Vec2i(gateP2.X - pillarW, gateP1.Y + lintelH);
        var rp2 = new Vec2i(gateP2.X, gateP2.Y - pedestalH);
        GameEngine.DrawBox(rp1, rp2, dCfg.StoneBase, true);
        GameEngine.DrawLine(rp1, new Vec2i(rp1.X, rp2.Y), dCfg.StoneHighlight);
        GameEngine.DrawLine(new Vec2i(rp2.X, rp1.Y), rp2, dCfg.StoneShadow);

        // 4. 門楣頂梁（Lintel）
        var lintelP1 = new Vec2i(gateP1.X - dCfg.LintelExtraWidth, gateP1.Y);
        var lintelP2 = new Vec2i(gateP2.X + dCfg.LintelExtraWidth, gateP1.Y + lintelH);
        GameEngine.DrawBox(lintelP1, lintelP2, dCfg.StoneBase, true);
        GameEngine.DrawLine(lintelP1, new Vec2i(lintelP2.X, lintelP1.Y), dCfg.StoneHighlight);
        GameEngine.DrawLine(new Vec2i(lintelP1.X, lintelP2.Y), lintelP2, dCfg.StoneShadow);
        GameEngine.DrawBox(lintelP1, lintelP2, inRange ? itemCfg.RuneColorActive : dCfg.StoneHighlight, false);
    }

    private static void DrawShrineRunes(
        Vec2i gateP1,
        Vec2i gateP2,
        int pillarW,
        int lintelH,
        int pedestalH,
        TutorialDoorsConfig dCfg,
        TutorialDoorItemConfig itemCfg,
        bool inRange,
        float animTimer)
    {
        var runeColor = inRange ? itemCfg.RuneColorActive : itemCfg.RuneColorIdle;
        var symbols = itemCfg.RuneSymbols.AsSpan();
        if (symbols.IsEmpty) return;

        var startY = gateP1.Y + lintelH + dCfg.RuneMarginY;
        var bottomY = gateP2.Y - pedestalH - dCfg.RuneMarginY;
        var spanY = Math.Max(dCfg.RuneMinSpanY, bottomY - startY);
        var step = spanY / dCfg.RuneCount;

        var leftX = gateP1.X + pillarW / 2;
        var rightX = gateP2.X - pillarW / 2;

        for (var i = 0; i < dCfg.RuneCount; i++)
        {
            var y = startY + i * step;
            var glyphIdx = (i * dCfg.RuneGlyphStep) % symbols.Length;
            var runeChar = symbols.Slice(glyphIdx, 1);

            GameEngine.DrawCenterString(new Vec2i(leftX, y), runeChar, runeColor);
            GameEngine.DrawCenterString(new Vec2i(rightX, y), runeChar, runeColor);
        }
    }

    private static void DrawBossAuraAndMotion(
        Vec2i center,
        BossType bossType,
        int groundY,
        TutorialDoorsConfig dCfg,
        TutorialDoorItemConfig itemCfg,
        bool inRange,
        float animTimer,
        TutorialColorsConfig colors,
        IAssetManager assets)
    {
        var isSnake = bossType == BossType.BossSnake;
        var hasTex = assets.TryGetTexture(itemCfg.TexturePath, out var texHandle);
        Debug.Assert(hasTex, $"Boss 扉テクスチャが見つかりません: {itemCfg.TexturePath}");
        if (!hasTex) return;

        if (isSnake)
        {
            var snakeCfg = dCfg.SnakeMotion;
            var bobY = MathF.Sin(animTimer * itemCfg.MotionSpeed) * itemCfg.MotionAmplitude;
            var bobX = MathF.Cos(animTimer * itemCfg.MotionSpeed * snakeCfg.BobSpeedMultiplierX) * (itemCfg.MotionAmplitude * snakeCfg.BobAmplitudeMultiplierX);
            var previewPos = new Vec2i((int)(center.X + bobX), (int)(center.Y + snakeCfg.PreviewOffsetY + bobY));

            GameEngine.DrawRotaGraph3(previewPos, dCfg.PreviewScale, 0.0, texHandle, true);

            // 蛇神光輪：周囲を旋回する浮遊毒霊オーブ
            for (var k = 0; k < snakeCfg.OrbCount; k++)
            {
                var theta = animTimer * snakeCfg.OrbSpeed + k * (MathF.PI * 2f / snakeCfg.OrbCount);
                var orbX = (int)(previewPos.X + MathF.Cos(theta) * snakeCfg.OrbRadius.X);
                var orbY = (int)(previewPos.Y + MathF.Sin(theta) * snakeCfg.OrbRadius.Y);
                var orbR = inRange ? snakeCfg.OrbActiveRadius : snakeCfg.OrbIdleRadius;
                var orbCol = inRange ? itemCfg.RuneColorActive : itemCfg.AuraColor;

                GameEngine.DrawCircle(new Vec2i(orbX, orbY), orbR, orbCol, true);
                if (inRange)
                {
                    GameEngine.DrawCircle(new Vec2i(orbX, orbY), orbR + snakeCfg.OrbGlowDelta, itemCfg.AuraColor, false);
                }
            }
        }
        else
        {
            // 獅子神動態：重砸地面＆衝撃波（Ground Slam & Shockwave）
            var lionCfg = dCfg.LionMotion;
            var t = (animTimer * itemCfg.MotionSpeed) % 1.0f;
            float slamY;
            float sx;
            float sy;
            var shockProgress = -1f;

            if (t < lionCfg.IdlePhaseEnd)
            {
                // 1. 威圧伺機姿態：低周波の重厚な呼吸脈動
                var breath = MathF.Sin(t * MathF.PI * lionCfg.BreathFrequency) * lionCfg.BreathAmplitude;
                slamY = 0f;
                sx = 1f + breath;
                sy = 1f - breath;
            }
            else if (t < lionCfg.JumpPhaseEnd)
            {
                // 2. 猛獅起跳蓄力 (Ease-out 騰空)
                var duration = lionCfg.JumpPhaseEnd - lionCfg.IdlePhaseEnd;
                var p = (t - lionCfg.IdlePhaseEnd) / duration;
                var h = MathF.Sin(p * (MathF.PI * 0.5f));
                slamY = -h * itemCfg.MotionAmplitude;
                sx = 1f - lionCfg.JumpStretchX * h;
                sy = 1f + lionCfg.JumpStretchY * h;
            }
            else if (t < lionCfg.PeakPhaseEnd)
            {
                // 3. 頂点凝滞（威圧定格）
                slamY = -itemCfg.MotionAmplitude;
                sx = lionCfg.PeakScaleX;
                sy = lionCfg.PeakScaleY;
            }
            else if (t < lionCfg.FallPhaseEnd)
            {
                // 4. 流星重砸下落 (Ease-in 3次加速下墜)
                var duration = lionCfg.FallPhaseEnd - lionCfg.PeakPhaseEnd;
                var p = (t - lionCfg.PeakPhaseEnd) / duration;
                slamY = -(1f - p * p * p) * itemCfg.MotionAmplitude;
                sx = 1f - lionCfg.FallStretchX * p;
                sy = 1f + lionCfg.FallStretchY * p;
            }
            else
            {
                // 5. 触地重砸、強烈圧扁形変および減衰反発
                var duration = 1.0f - lionCfg.FallPhaseEnd;
                var p = (t - lionCfg.FallPhaseEnd) / duration;
                var decay = 1f - p;
                var squash = decay * MathF.Cos(p * MathF.PI * lionCfg.SquashOscillation);
                slamY = 0f;
                sx = 1f + lionCfg.SquashAmplitude * squash;
                sy = 1f - lionCfg.SquashAmplitude * squash;
                shockProgress = p;
            }

            var baseScale = dCfg.PreviewScale;
            var halfH = lionCfg.TextureHeight * baseScale.Y * 0.5f;
            var footCompY = (1f - sy) * halfH;

            // 着地瞬間の微幅な衝撃震動
            var impactTremor = 0;
            if (shockProgress is >= 0f && shockProgress < lionCfg.TremorDuration)
            {
                impactTremor = (int)(MathF.Sin(shockProgress * lionCfg.TremorFrequency) * (1f - shockProgress / lionCfg.TremorDuration) * lionCfg.TremorAmplitude);
            }

            var previewPos = new Vec2i(center.X, (int)(center.Y + lionCfg.PreviewOffsetY + slamY + footCompY + impactTremor));
            var scale = new Vec2f(baseScale.X * sx, baseScale.Y * sy);

            GameEngine.DrawRotaGraph3(previewPos, scale, 0.0, texHandle, true);

            // 6. Ground Shockwave
            if (shockProgress >= 0f)
            {
                var sp = shockProgress;
                var alpha = (int)((1f - sp) * lionCfg.MaxShockwaveAlpha);
                if (alpha > 0)
                {
                    GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);

                    var groundCenter = new Vec2i(center.X, groundY - lionCfg.GroundOffsetY);
                    var spread = (int)(sp * lionCfg.MaxSpread);
                    var waveCol = itemCfg.RuneColorActive;
                    var coreCol = itemCfg.AuraColor;

                    // A. 地表を左右へ高速走駆する横向激波刃
                    var originOffset = lionCfg.WaveOriginOffset;
                    GameEngine.DrawLine(new Vec2i(groundCenter.X - originOffset, groundCenter.Y), new Vec2i(groundCenter.X - originOffset - spread, groundCenter.Y), waveCol, lionCfg.WaveThickness);
                    GameEngine.DrawLine(new Vec2i(groundCenter.X + originOffset, groundCenter.Y), new Vec2i(groundCenter.X + originOffset + spread, groundCenter.Y), waveCol, lionCfg.WaveThickness);

                    // B. 激波前沿の地裂衝天尖刺
                    var spikeH1 = (int)((1f - sp) * lionCfg.Spike1Height);
                    var spikeH2 = (int)((1f - sp) * lionCfg.Spike2Height);
                    var s1Offset = originOffset + lionCfg.Spike1Offset;
                    GameEngine.DrawLine(new Vec2i(groundCenter.X - originOffset - spread, groundCenter.Y), new Vec2i(groundCenter.X - s1Offset - spread, groundCenter.Y - spikeH1), coreCol, lionCfg.SpikeThickness);
                    GameEngine.DrawLine(new Vec2i(groundCenter.X + originOffset + spread, groundCenter.Y), new Vec2i(groundCenter.X + s1Offset + spread, groundCenter.Y - spikeH1), coreCol, lionCfg.SpikeThickness);

                    var midSpread = (int)(spread * 0.5f);
                    var s2Offset = originOffset + lionCfg.Spike2Offset;
                    GameEngine.DrawLine(new Vec2i(groundCenter.X - originOffset - midSpread, groundCenter.Y), new Vec2i(groundCenter.X - s2Offset - midSpread, groundCenter.Y - spikeH2), coreCol, lionCfg.SpikeThickness);
                    GameEngine.DrawLine(new Vec2i(groundCenter.X + originOffset + midSpread, groundCenter.Y), new Vec2i(groundCenter.X + s2Offset + midSpread, groundCenter.Y - spikeH2), coreCol, lionCfg.SpikeThickness);

                    // C. 上空へ噴流拡散する半月形高圧激波弧
                    var arcR = (int)(lionCfg.ArcBaseRadius + sp * lionCfg.ArcSpread);
                    var prevPt = new Vec2i(groundCenter.X - (int)(arcR * lionCfg.ArcScale.X), groundCenter.Y);
                    for (var s = 1; s <= lionCfg.ArcSegments; s++)
                    {
                        var theta = -MathF.PI + s * (MathF.PI / lionCfg.ArcSegments);
                        var currPt = new Vec2i(
                            groundCenter.X + (int)(MathF.Cos(theta) * arcR * lionCfg.ArcScale.X),
                            groundCenter.Y + (int)(MathF.Sin(theta) * arcR * lionCfg.ArcScale.Y));
                        GameEngine.DrawLine(prevPt, currPt, waveCol, lionCfg.ArcThickness);
                        prevPt = currPt;
                    }

                    // D. 触地瞬間の高熱衝撃爆心フラッシュ
                    if (sp < lionCfg.FlashThreshold)
                    {
                        var flashA = (int)((1f - sp / lionCfg.FlashThreshold) * lionCfg.FlashMaxAlpha);
                        GameEngine.SetDrawBlendMode(BlendMode.Add, flashA);
                        GameEngine.DrawBox(
                            new Vec2i(groundCenter.X - lionCfg.FlashHalfSize.X, groundCenter.Y - lionCfg.FlashHalfSize.Y),
                            new Vec2i(groundCenter.X + lionCfg.FlashHalfSize.X, groundCenter.Y + lionCfg.FlashHalfSize.Y),
                            colors.White,
                            true);
                    }

                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                }
            }
        }
    }

    private static void DrawShrinePlaqueAndPrompt(
        Vec2i center,
        Vec2i gateP1,
        TutorialDoorsConfig dCfg,
        TutorialDoorItemConfig itemCfg,
        bool inRange,
        float animTimer,
        string promptText,
        TutorialColorsConfig colors)
    {
        // 門楣上の神殿銘板プレート
        GameEngine.DrawCenterString(new Vec2i(center.X, gateP1.Y + dCfg.PlaqueTitleOffsetY), itemCfg.Title, colors.White);
        GameEngine.DrawCenterString(new Vec2i(center.X, gateP1.Y + dCfg.PlaqueTagOffsetY), itemCfg.Tag, itemCfg.RuneColorActive);

        // 接近時の浮遊 [F] 挑戦開始プロンプト枠
        if (inRange)
        {
            var promptPos = new Vec2i(center.X, gateP1.Y + dCfg.TooltipOffsetY);
            var pulse = (int)(MathF.Sin(animTimer * dCfg.TooltipPulseSpeed) * dCfg.TooltipPulseAmplitude);
            var halfTipW = dCfg.TooltipSize.X / 2;
            var halfTipH = dCfg.TooltipSize.Y / 2;
            var bubbleP1 = new Vec2i(promptPos.X - halfTipW, promptPos.Y - halfTipH + pulse);
            var bubbleP2 = new Vec2i(promptPos.X + halfTipW, promptPos.Y + halfTipH + pulse);

            GameEngine.DrawBox(bubbleP1, bubbleP2, colors.TooltipBg, true);
            GameEngine.DrawBox(bubbleP1, bubbleP2, itemCfg.RuneColorActive, false);
            GameEngine.DrawBox(bubbleP1 - Vec2i.One, bubbleP2 + Vec2i.One, itemCfg.RuneColorActive, false);

            GameEngine.DrawCenterString(promptPos + new Vec2i(0, pulse), promptText, colors.Gold);
        }
    }
}
