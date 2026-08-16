using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Stages;

namespace AnimalBattle.Systems;
/// <summary>
/// Native C++ 描画 API (DxLib) を P/Invoke 経由で呼び出し、背景・足場・2D スプライト・HUD 情報を描画する Flecs.NET ECS システム群。
/// </summary>
public static class RenderSystems
{
    /// <summary>
    /// 画面クリア、背景描画、足場（Platform）描画、および Player/Effect の 2D スプライト描画を実行します。
    /// </summary>
    public static readonly Action<World> RenderSystem = (world) =>
    {
        world.QueryBuilder<Transform, Sprite>()
             .Build()
             .Each((ref Transform trans, ref Sprite sprite) =>
             {
                 if (!sprite.IsVisible || sprite.GraphHandle < 0)
                 {
                     return;
                 }

                 int destX = (int)(trans.X - (sprite.SrcRect.Width * trans.ScaleX) / 2.0f);
                 int destY = (int)(trans.Y - (sprite.SrcRect.Height * trans.ScaleY) / 2.0f);

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
             });

        world.QueryBuilder<Transform, CollisionBox>()
             .With<PlatformTag>()
             .Build()
             .Each((ref Transform trans, ref CollisionBox col) =>
             {
                 int x1 = (int)(trans.X - col.Width / 2.0f);
                 int y1 = (int)(trans.Y - col.Height / 2.0f);
                 int x2 = (int)(trans.X + col.Width / 2.0f);
                 int y2 = (int)(trans.Y + col.Height / 2.0f);

                 StageInfo stage = StageRegistry.CurrentStage;
                 uint fillColor = 0xFF1E5128;
                 uint outlineColor = 0xFF4E9F3D;

                 if (stage.Platforms.Count > 0)
                 {
                     fillColor = stage.Platforms[0].FillColor;
                     outlineColor = stage.Platforms[0].OutlineColor;
                 }

                 GameEngine.DrawBox(x1, y1, x2, y2, fillColor, true);
                 GameEngine.DrawBox(x1, y1, x2, y2, outlineColor, false);
             });
    };

    /// <summary>
    /// ステージ情報、操作ガイド、およびプレイヤーの蓄積ダメージ％ HUD を画面上に描画します。
    /// </summary>
    public static readonly Action<World> DebugInfoSystem = (world) =>
    {
        StageInfo stage = StageRegistry.CurrentStage;

        GameEngine.DrawString(20, 20, $"=== マップ: {stage.Name} (ID: {stage.Id}) ===", stage.ThemeColor);
        GameEngine.DrawString(20, 40, $"{stage.Subtitle}", 0xFFAAAAAA);
        GameEngine.DrawString(20, 60, "[操作方法] P1: WASD+J | P2: 方向キー+L | P3/P4: パッド3/4 | [1-4 / M]: マップ切替", 0xFFDDDDDD);

        world.QueryBuilder<PlayerId, CharacterStats>()
             .With<PlayerTag>()
             .Build()
             .Each((ref PlayerId pid, ref CharacterStats stats) =>
             {
                 int id = pid.Id;
                 int cardX = 200 + (id - 1) * 240;
                 const int hudY = 620;

                 uint color = id switch
                 {
                     1 => 0xFF3498DB,
                     2 => 0xFF9B59B6,
                     3 => 0xFFE74C3C,
                     4 => 0xFF2ECC71,
                     _ => 0xFFFFFFFF
                 };

                 GameEngine.DrawBox(cardX - 10, hudY - 5, cardX + 180, hudY + 50, 0xAA000000, true);
                 GameEngine.DrawBox(cardX - 10, hudY - 5, cardX + 180, hudY + 50, color, false);
                 GameEngine.DrawString(cardX, hudY, $"プレイヤー {id}", color);
                 GameEngine.DrawString(cardX, hudY + 24, $"ダメージ: {stats.DamagePercent:F0}%", stats.DamagePercent > 100.0f ? 0xFFFF3333 : 0xFFFFFFFF);
             });
    };
}
