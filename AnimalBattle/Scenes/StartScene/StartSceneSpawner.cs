namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面のエンティティツリー：自機プレビューおよび UI 状態を生成・解体するライフサイクル装配クラス。
/// </summary>
public static class StartSceneSpawner {
    private const string RootName = "StartSceneRoot";

    /// <summary>
    ///     開始画面のルートエンティティおよびプレビュー自機をワールドへ装配します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="enableTutorialOverride">チュートリアル有効化チェックボックスの明示的な初期値オーバーライド（nullの場合は設定ファイル値を使用）</param>
    public static void Spawn(World world, bool? enableTutorialOverride = null)
    {
        Teardown(world);

        InputService.SetCursorVisible(true);

        var root = world.Entity(RootName).Add<StartSceneTag>();
        var assets = world.Get<IAssetManager>();
        var layout = world.Get<StartSceneConfig>().Layout;
        var catCardBounds = layout.CatCard.ToRectangle();
        var bunnyCardBounds = layout.BunnyCard.ToRectangle();

        // 1. 自機プレビューエンティティの生成 アニメーションシステムの機能を利用します
        assets.TryGetCharacterSpriteHandle(CharacterType.Cat, "Idle", out var catHandle);
        var catPreview = world.Entity("CatPreview")
            .ChildOf(root)
            .Add<StartSceneTag>()
            .Set(new Transform(new Vec2f(catCardBounds.X + catCardBounds.Width / 2f, catCardBounds.Y + layout.PreviewCenterOffsetY), new Vec2f(3.5f, 3.5f)))
            .Set(new Sprite(catHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new Animation { CharacterType = CharacterType.Cat, CurrentClip = "Idle", IsPlaying = true });

        assets.TryGetCharacterSpriteHandle(CharacterType.Bunny, "Idle", out var bunnyHandle);
        var bunnyPreview = world.Entity("BunnyPreview")
            .ChildOf(root)
            .Add<StartSceneTag>()
            .Set(new Transform(new Vec2f(bunnyCardBounds.X + bunnyCardBounds.Width / 2f, bunnyCardBounds.Y + layout.PreviewCenterOffsetY), new Vec2f(3.5f, 3.5f)))
            .Set(new Sprite(bunnyHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new Animation { CharacterType = CharacterType.Bunny, CurrentClip = "Idle", IsPlaying = true });

        // 2. UI 状態コンポーネントの設定
        var startSceneCfg = world.Get<StartSceneConfig>() ?? new StartSceneConfig();
        var isTutorialEnabled = enableTutorialOverride ?? startSceneCfg.Options.DefaultEnableTutorial;
        root.Set(new StartUiState {
            CatPreviewEntity = catPreview,
            BunnyPreviewEntity = bunnyPreview,
            IsTutorialEnabled = isTutorialEnabled
        });
    }

    /// <summary>
    ///     開始画面のルートエンティティおよび全プレビューエンティティを一括破棄します。
    ///     プレビュー個別破棄、StartSceneTag クエリ一括破棄、およびルート破棄による多層防護で残留を完全に防止します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        var cat = world.Lookup("CatPreview");
        if (cat.IsAlive())
        {
            cat.Destruct();
        }

        var bunny = world.Lookup("BunnyPreview");
        if (bunny.IsAlive())
        {
            bunny.Destruct();
        }

        using var startQuery = world.QueryBuilder().With<StartSceneTag>().Build();
        startQuery.Each(static e =>
        {
            if (e.IsAlive())
            {
                e.Destruct();
            }
        });

        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
