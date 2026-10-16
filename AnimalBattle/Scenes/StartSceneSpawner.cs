using System.Drawing;
using AnimalBattle.Asset;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     ゲーム開始・選人シーンのエンティティツリー（自機プレビューおよび UI 状態）を生成・解体するライフサイクル装配クラス。
/// </summary>
public static class StartSceneSpawner {
    private const string RootName = "StartSceneRoot";

    private static StartSceneLayoutConfig Layout => ConfigManager.StartScene.Layout;
    private static Rectangle CatCardBounds => Layout.CatCard.ToRectangle();
    private static Rectangle BunnyCardBounds => Layout.BunnyCard.ToRectangle();

    /// <summary>
    ///     開始画面のルートエンティティおよびプレビュー自機をワールドへ装配します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Spawn(World world)
    {
        Teardown(world);

        InputService.SetCursorVisible(true);

        var root = world.Entity(RootName).Add<StartSceneTag>();

        // 1. 自機プレビューエンティティの生成 (AnimationModule / SpriteAnimationSystem の恩恵を享受)
        AssetManager.TryGetCharacterSpriteHandle(CharacterType.Cat, "Idle", out var catHandle);
        var catPreview = world.Entity("CatPreview")
            .ChildOf(root)
            .Set(new Transform(CatCardBounds.X + CatCardBounds.Width / 2f, CatCardBounds.Y + Layout.PreviewCenterOffsetY, 3.5f, 3.5f))
            .Set(new Sprite(catHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new Animation { CharacterType = CharacterType.Cat, CurrentClip = "Idle", IsPlaying = true });

        AssetManager.TryGetCharacterSpriteHandle(CharacterType.Bunny, "Idle", out var bunnyHandle);
        var bunnyPreview = world.Entity("BunnyPreview")
            .ChildOf(root)
            .Set(new Transform(BunnyCardBounds.X + BunnyCardBounds.Width / 2f, BunnyCardBounds.Y + Layout.PreviewCenterOffsetY, 3.5f, 3.5f))
            .Set(new Sprite(bunnyHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new Animation { CharacterType = CharacterType.Bunny, CurrentClip = "Idle", IsPlaying = true });

        // 2. 首領テクスチャハンドルの取得と UI 状態コンポーネントの設定
        AssetManager.TryGetTexture(ResourcePath.Characters.Character031_3Png, out var boss1TextureHandle);
        AssetManager.TryGetTexture(ResourcePath.Characters.Character027_3Png, out var boss2TextureHandle);

        root.Set(new StartUiState {
            CatPreviewEntity = catPreview,
            BunnyPreviewEntity = bunnyPreview,
            Boss1TextureHandle = boss1TextureHandle,
            Boss2TextureHandle = boss2TextureHandle
        });
    }

    /// <summary>
    ///     開始画面のルートエンティティおよび全プレビューエンティティを一括破棄します。
    ///     ルート破棄により、配下のプレビューエンティティおよび StartUiState は一括カスケード消滅します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public static void Teardown(World world)
    {
        var root = world.Lookup(RootName);
        if (root.IsAlive())
        {
            root.Destruct();
        }
    }
}
