using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Asset;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     ゲーム開始・選人・選首領シーン。
///     純粋な ECS 状態機モデルに基づき、プレビュー自機を Entity として生成してアニメーション管線を共有し、
///     自機・首領の選択および焦点アニメーション（Cat/Bunny 奔走、Boss1 浮遊飛行、Boss2 騰空重撃）を統括します。
/// </summary>
public sealed class StartScene : IDisposable {
    private static StartSceneLayoutConfig Layout => ConfigManager.StartScene.Layout;
    private static StartSceneColorsConfig Colors => ConfigManager.StartScene.Colors;

    private static Rectangle CatCardBounds => Layout.CatCard.ToRectangle();
    private static Rectangle BunnyCardBounds => Layout.BunnyCard.ToRectangle();
    private static Rectangle Boss1CardBounds => Layout.Boss1Card.ToRectangle();
    private static Rectangle Boss2CardBounds => Layout.Boss2Card.ToRectangle();
    private static Rectangle ConfirmButtonBounds => Layout.ConfirmButton.ToRectangle();

    // 配色定義 (ConfigManager.StartScene.Colors より動的取得)
    private static Color BgColor => Colors.BgColor;
    private static Color CardBgColor => Colors.CardBgColor;
    private static Color CardHoverBgColor => Colors.CardHoverBgColor;
    private static Color BorderDefaultColor => Colors.BorderDefaultColor;
    private static Color PlayerSelectedColor => Colors.PlayerSelectedColor;
    private static Color BossSelectedColor => Colors.BossSelectedColor;
    private static Color ConfirmBtnDefaultColor => Colors.ConfirmBtnDefaultColor;
    private static Color ConfirmBtnHoverColor => Colors.ConfirmBtnHoverColor;
    private static Color TextWhite => Colors.TextWhite;
    private static Color TextMuted => Colors.TextMuted;
    private static Color TextGold => Colors.TextGold;

    private World _ecsWorld = null!;
    private Entity _catPreviewEntity;
    private Entity _bunnyPreviewEntity;
    private int _boss1TextureHandle = -1;
    private int _boss2TextureHandle = -1;
    private bool _isDisposed;
    private bool _isActive;

    // 焦点動效タイマー
    private float _catStepTimer;
    private float _bunnyStepTimer;
    private float _boss1FlyTimer;
    private float _boss2SlamTimer;
    private bool _isBoss1Hovered;
    private bool _isBoss2Hovered;
    private bool _isConfirmHovered;

    /// <summary>
    ///     プレイヤーによって選択されたキャラクター種別。
    /// </summary>
    public CharacterType SelectedPlayer { get; private set; } = CharacterType.Cat;

    /// <summary>
    ///     プレイヤーによって選択された首領種別。
    /// </summary>
    public BossType SelectedBoss { get; private set; } = BossType.Boss1;

    /// <summary>
    ///     戦闘開始確定コールバック。
    /// </summary>
    public Action<CharacterType, BossType>? OnConfirmStart { get; set; }

    /// <summary>
    ///     開始画面シーンを構築し、プレビュー自機エンティティを生成します。
    /// </summary>
    public void Setup(World ecsWorld)
    {
        _ecsWorld = ecsWorld;
        _isActive = true;

        InputService.SetCursorVisible(true);

        var root = _ecsWorld.Entity("StartSceneRoot").Add<StartSceneTag>();

        // 1. 自機プレビューエンティティの生成 (AnimationModule / SpriteAnimationSystem の恩恵を享受)
        AssetManager.TryGetCharacterSpriteHandle(CharacterType.Cat, "Idle", out var catHandle);
        _catPreviewEntity = _ecsWorld.Entity("CatPreview")
            .ChildOf(root)
            .Set(new Transform(CatCardBounds.X + CatCardBounds.Width / 2f, CatCardBounds.Y + 160f, 3.5f, 3.5f))
            .Set(new Sprite { GraphHandle = catHandle, SrcRect = new Rect(0, 0, 32, 32) })
            .Set(new Animation { CharacterType = CharacterType.Cat, CurrentClip = "Idle", IsPlaying = true });

        AssetManager.TryGetCharacterSpriteHandle(CharacterType.Bunny, "Idle", out var bunnyHandle);
        _bunnyPreviewEntity = _ecsWorld.Entity("BunnyPreview")
            .ChildOf(root)
            .Set(new Transform(BunnyCardBounds.X + BunnyCardBounds.Width / 2f, BunnyCardBounds.Y + 160f, 3.5f, 3.5f))
            .Set(new Sprite { GraphHandle = bunnyHandle, SrcRect = new Rect(0, 0, 32, 32) })
            .Set(new Animation { CharacterType = CharacterType.Bunny, CurrentClip = "Idle", IsPlaying = true });

        // 2. 首領テクスチャのロード
        AssetManager.TryGetTexture(ResourcePath.Characters.Character031_3Png, out _boss1TextureHandle);
        AssetManager.TryGetTexture(ResourcePath.Characters.Character027_3Png, out _boss2TextureHandle);
    }

    /// <summary>
    ///     マウス入力および焦点動效タイマーを更新します。
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        var mousePos = InputService.GetMousePosition();
        var isClick = InputService.IsPressed(InputAction.MouseClick);
        var isConfirmKey = InputService.IsPressed(InputAction.Confirm);

        // --- 1. Cat カード判定 ---
        var isCatHovered = CatCardBounds.Contains(mousePos.X, mousePos.Y);
        if (_catPreviewEntity.IsAlive())
        {
            ref var anim = ref _catPreviewEntity.GetMut<Animation>();
            if (isCatHovered)
            {
                anim.Play("Run");
                _catStepTimer += deltaTime;
            }
            else
            {
                anim.Play("Idle");
                _catStepTimer = 0f;
            }
        }
        if (isCatHovered && isClick)
        {
            SelectedPlayer = CharacterType.Cat;
        }

        // --- 2. Bunny カード判定 ---
        var isBunnyHovered = BunnyCardBounds.Contains(mousePos.X, mousePos.Y);
        if (_bunnyPreviewEntity.IsAlive())
        {
            ref var anim = ref _bunnyPreviewEntity.GetMut<Animation>();
            if (isBunnyHovered)
            {
                anim.Play("Run");
                _bunnyStepTimer += deltaTime;
            }
            else
            {
                anim.Play("Idle");
                _bunnyStepTimer = 0f;
            }
        }
        if (isBunnyHovered && isClick)
        {
            SelectedPlayer = CharacterType.Bunny;
        }

        // --- 3. Boss 1 カード判定 (飛行・浮遊動效) ---
        _isBoss1Hovered = Boss1CardBounds.Contains(mousePos.X, mousePos.Y);
        if (_isBoss1Hovered)
        {
            _boss1FlyTimer += deltaTime;
            if (isClick) SelectedBoss = BossType.Boss1;
        }
        else
        {
            // 非ホバー時は徐々にホームポジションへ減衰
            _boss1FlyTimer = Math.Max(0f, _boss1FlyTimer - deltaTime * 2f);
        }

        // --- 4. Boss 2 カード判定 (騰空・重撃下砸動效) ---
        _isBoss2Hovered = Boss2CardBounds.Contains(mousePos.X, mousePos.Y);
        if (_isBoss2Hovered)
        {
            _boss2SlamTimer += deltaTime;
            if (_boss2SlamTimer > 1.1f) _boss2SlamTimer -= 1.1f; // 1.1秒周期でループ
            if (isClick) SelectedBoss = BossType.Boss2;
        }
        else
        {
            _boss2SlamTimer = 0f;
        }

        // --- 5. 確定ボタン判定 ---
        _isConfirmHovered = ConfirmButtonBounds.Contains(mousePos.X, mousePos.Y);
        if ((_isConfirmHovered && isClick) || isConfirmKey)
        {
            OnConfirmStart?.Invoke(SelectedPlayer, SelectedBoss);
        }
    }

    /// <summary>
    ///     選人画面および各種視覚動效を描画します。
    /// </summary>
    public void Render()
    {
        if (!_isActive) return;

        // 1. 背景クリア
        GameEngine.DrawBox(0, 0, 1280, 720, BgColor, true);

        // 2. タイトルヘッダー描画
        GameEngine.DrawString(430, 40, "ANIMAL BATTLE - キャラクター＆ボス選択", TextGold);
        GameEngine.DrawString(240, 105, "【 プレイヤー選択 】", TextWhite);
        GameEngine.DrawString(820, 105, "【 ボス選択 】", TextWhite);

        // 3. Cat カード描画
        DrawPlayerCard(
            CatCardBounds,
            "キャット (Cat)",
            "【 突進 ＆ 遠距離攻撃 】",
            "鋭いダッシュ回避と遠距離弾幕射撃",
            SelectedPlayer == CharacterType.Cat,
            _catPreviewEntity,
            _catStepTimer);

        // 4. Bunny カード描画
        DrawPlayerCard(
            BunnyCardBounds,
            "バニー (Bunny)",
            "【 二段ジャンプ ＆ 近接攻撃 】",
            "軽快な二段ジャンプと大剣近接斬撃",
            SelectedPlayer == CharacterType.Bunny,
            _bunnyPreviewEntity,
            _bunnyStepTimer);

        // 5. Boss 1 カード描画 (飛行浮遊)
        DrawBoss1Card(
            Boss1CardBounds,
            "ボス 1 (Boss 1)",
            "【 飛行浮遊 ＆ 全方位弾幕 】",
            SelectedBoss == BossType.Boss1,
            _boss1FlyTimer);

        // 6. Boss 2 カード描画 (騰空下砸)
        DrawBoss2Card(
            Boss2CardBounds,
            "ボス 2 (Boss 2)",
            "【 突撃突進 ＆ 衝撃波重撃 】",
            SelectedBoss == BossType.Boss2,
            _boss2SlamTimer);

        // 7. 確定ボタン描画
        DrawConfirmButton();

        // 8. 下部ヒントテキスト
        GameEngine.DrawString(330, 665, "※ マウスホバーでアクション確認 / クリックで選択 / [ENTER] キーで戦闘開始", TextMuted);
    }

    private void DrawPlayerCard(
        Rectangle bounds,
        string title,
        string tag,
        string desc,
        bool isSelected,
        Entity previewEntity,
        float stepTimer)
    {
        var borderColor = isSelected ? PlayerSelectedColor : BorderDefaultColor;
        var borderThickness = isSelected ? 3 : 1;

        // カード外枠
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (borderThickness > 1)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        // キャラクタータイトル＆特徴タグ
        GameEngine.DrawString(bounds.Left + 20, bounds.Top + 20, title, isSelected ? PlayerSelectedColor : TextWhite);
        GameEngine.DrawString(bounds.Left + 15, bounds.Top + 55, tag, TextGold);

        // プレビュー自機スプライト描画
        if (previewEntity.IsAlive() && previewEntity.Has<Sprite>())
        {
            ref readonly var sprite = ref previewEntity.Get<Sprite>();
            ref readonly var transform = ref previewEntity.Get<Transform>();

            // ホバー時の小幅左右ステップ振動
            var stepOffset = stepTimer > 0f ? MathF.Sin(stepTimer * 14f) * 4f : 0f;

            var drawCenterX = (int)(transform.X + stepOffset);
            var drawCenterY = (int)transform.Y;

            GameEngine.DrawRectRotaGraph(
                new Vec2i(drawCenterX, drawCenterY),
                new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top),
                new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height),
                transform.ScaleX,
                transform.ScaleY,
                0.0,
                sprite.GraphHandle,
                true,
                false);
        }

        // 選択状態バッジ
        var badgeText = isSelected ? "[ 選択中 - SELECTED ]" : "[ クリックで選択 ]";
        var badgeColor = isSelected ? PlayerSelectedColor : TextMuted;
        GameEngine.DrawString(bounds.Left + 30, bounds.Bottom - 65, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + 10, bounds.Bottom - 35, desc, TextMuted);
    }

    private void DrawBoss1Card(
        Rectangle bounds,
        string title,
        string tag,
        bool isSelected,
        float flyTimer)
    {
        var borderColor = isSelected ? BossSelectedColor : BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + 20, bounds.Top + 20, title, isSelected ? BossSelectedColor : TextWhite);
        GameEngine.DrawString(bounds.Left + 15, bounds.Top + 55, tag, TextGold);

        if (_boss1TextureHandle >= 0)
        {
            // 飛行浮遊動效：上昇 + 正弦波浮動 + 微傾角
            var flyOffsetY = flyTimer > 0f ? MathF.Sin(flyTimer * 5f) * 10f - 35f : 0f;
            var tiltAngle = flyTimer > 0f ? MathF.Sin(flyTimer * 2.5f) * 0.05f : 0f;

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + 160 + flyOffsetY);

            GameEngine.DrawRotaGraph(new Vec2i(drawX, drawY), 2.2, tiltAngle, _boss1TextureHandle, true);
        }

        var badgeText = isSelected ? "[ 選択中 - SELECTED ]" : "[ クリックで選択 ]";
        var badgeColor = isSelected ? BossSelectedColor : TextMuted;
        GameEngine.DrawString(bounds.Left + 30, bounds.Bottom - 65, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + 25, bounds.Bottom - 35, "※ ホバーで飛行・浮遊動效", TextMuted);
    }

    private void DrawBoss2Card(
        Rectangle bounds,
        string title,
        string tag,
        bool isSelected,
        float slamTimer)
    {
        var borderColor = isSelected ? BossSelectedColor : BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + 20, bounds.Top + 20, title, isSelected ? BossSelectedColor : TextWhite);
        GameEngine.DrawString(bounds.Left + 15, bounds.Top + 55, tag, TextGold);

        if (_boss2TextureHandle >= 0)
        {
            // 騰空・重撃下砸動效：
            // 0.00 ~ 0.25s: 蓄力跳躍上昇 45px
            // 0.25 ~ 0.35s: 高速下砸
            // 0.35 ~ 0.50s: 触地擠圧変形 (ScaleX 1.25, ScaleY 0.75) + 微震
            // 0.50 ~ 1.10s: 復帰待機
            var offsetY = 0f;
            var scaleX = 2.2;
            var scaleY = 2.2;

            if (slamTimer > 0f)
            {
                if (slamTimer <= 0.25f)
                {
                    // 上昇
                    offsetY = -45f * (slamTimer / 0.25f);
                    scaleX = 2.0;
                    scaleY = 2.4;
                }
                else if (slamTimer <= 0.35f)
                {
                    // 下砸
                    var t = (slamTimer - 0.25f) / 0.10f;
                    offsetY = -45f * (1f - t);
                    scaleX = 2.1;
                    scaleY = 2.3;
                }
                else if (slamTimer <= 0.50f)
                {
                    // 触地擠圧
                    var t = (slamTimer - 0.35f) / 0.15f;
                    offsetY = MathF.Sin(t * MathF.PI * 4f) * 2f; // 微震
                    scaleX = 2.2 * (1.25 - 0.25 * t);
                    scaleY = 2.2 * (0.75 + 0.25 * t);
                }
            }

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + 160 + offsetY);

            // DrawRectRotaGraph 等価描画または DrawRotaGraph
            GameEngine.DrawRotaGraph(new Vec2i(drawX, drawY), scaleX, 0.0, _boss2TextureHandle, true);
        }

        var badgeText = isSelected ? "[ 選択中 - SELECTED ]" : "[ クリックで選択 ]";
        var badgeColor = isSelected ? BossSelectedColor : TextMuted;
        GameEngine.DrawString(bounds.Left + 30, bounds.Bottom - 65, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + 25, bounds.Bottom - 35, "※ ホバーで騰空・重撃動效", TextMuted);
    }

    private void DrawConfirmButton()
    {
        var btnColor = _isConfirmHovered ? ConfirmBtnHoverColor : ConfirmBtnDefaultColor;
        var borderColor = _isConfirmHovered ? Color.FromArgb(100, 200, 255) : Color.FromArgb(60, 120, 180);

        GameEngine.DrawBox(
            ConfirmButtonBounds.Left, ConfirmButtonBounds.Top,
            ConfirmButtonBounds.Right, ConfirmButtonBounds.Bottom,
            btnColor, true);

        GameEngine.DrawBox(
            ConfirmButtonBounds.Left, ConfirmButtonBounds.Top,
            ConfirmButtonBounds.Right, ConfirmButtonBounds.Bottom,
            borderColor, false);

        if (_isConfirmHovered)
        {
            GameEngine.DrawBox(
                ConfirmButtonBounds.Left - 1, ConfirmButtonBounds.Top - 1,
                ConfirmButtonBounds.Right + 1, ConfirmButtonBounds.Bottom + 1,
                borderColor, false);
        }

        GameEngine.DrawString(ConfirmButtonBounds.Left + 55, ConfirmButtonBounds.Top + 20, "【 戦 闘 開 始 】 [ENTER]", TextWhite);
    }

    /// <summary>
    ///     開始画面を終了し、プレビュー自機エンティティ群を一括消滅させます。
    /// </summary>
    public void Teardown()
    {
        if (!_isActive) return;
        _isActive = false;

        var root = _ecsWorld.Lookup("StartSceneRoot");
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        Teardown();
        _isDisposed = true;
    }
}
