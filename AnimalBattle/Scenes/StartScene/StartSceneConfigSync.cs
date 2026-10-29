namespace AnimalBattle.Scenes.StartScene;

using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

/// <summary>
/// 開始画面（StartScene）設定変更時に、Flecs.NET ワールドの単例および画面プレビューエンティティを同期する同期機構。
/// </summary>
public static class StartSceneConfigSync
{
    /// <summary>
    /// 開始画面設定の変更をワールドおよびプレビューエンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の StartSceneConfig 設定</param>
    public static void Sync(World world, StartSceneConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "StartSceneConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 開始画面アクティブ時、プレビューエンティティの座標を再計算
        using var query = world.QueryBuilder<StartUiState>().With<StartSceneTag>().Build();
        query.Each((ref StartUiState uiState) =>
        {
            var layout = newConfig.Layout;
            var catBounds = layout.CatCard.ToRectangle();
            var bunnyBounds = layout.BunnyCard.ToRectangle();

            if (uiState.CatPreviewEntity.IsAlive())
            {
                ref var t = ref uiState.CatPreviewEntity.GetMut<Transform>();
                t.Position = new Vec2f(catBounds.X + catBounds.Width / 2f, catBounds.Y + layout.PreviewCenterOffsetY);
            }

            if (uiState.BunnyPreviewEntity.IsAlive())
            {
                ref var t = ref uiState.BunnyPreviewEntity.GetMut<Transform>();
                t.Position = new Vec2f(bunnyBounds.X + bunnyBounds.Width / 2f, bunnyBounds.Y + layout.PreviewCenterOffsetY);
            }
        });
    }
}
