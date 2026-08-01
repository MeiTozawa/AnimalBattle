using System;
using System.Collections.ObjectModel;
using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Inputs;
using AnimalBattle.Stages;

namespace AnimalBattle.Scenes;

public partial class BattleScene
{
    private void RenderHud(
        MapData currentMap,
        ReadOnlyCollection<Character> players,
        ReadOnlySpan<NativePhysicsObject> pObjs,
        float camX,
        float camY,
        float camScale)
    {
        int count = players.Count;

        // HUD UI 描画（カメラ変換オフ）
        _render.SetCameraEnabled(false);

        _render.DrawString(new Point<int>(30, 15), $"=== {currentMap.Name} ({count} Players) ===", currentMap.ThemeColor);
        _render.DrawString(new Point<int>(30, 37), $"{currentMap.Subtitle}", 0xFFCCCCCC);
        _render.DrawString(new Point<int>(30, 59), $"Camera: ({camX:F1}, {camY:F1}) | Zoom: {camScale:F2}x | Debug (F1): {(_showDebugBoxes ? "ON" : "OFF")}", 0xFF00FF00);

        // 全プレイヤーの HUD 情報表示 (画面下部に横並び配置)
        int hudWidth = 1200 / Math.Max(count, 1);
        for (int i = 0; i < count; i++)
        {
            var player = players[i];
            ref readonly var pObj = ref pObjs[i];

            int hudX = 30 + i * hudWidth;
            int hudY = 620;

            InputDevice device = _input.GetActiveDevice(player.PlayerIndex);
            string typeStr = player.PlayerType == PlayerType.Cpu ? "CPU" : device.ToString();
            _render.DrawString(new Point<int>(hudX, hudY), $"{player.Name} [{typeStr}] (Team {player.TeamId})", player.MainColor);
            _render.DrawString(new Point<int>(hudX, hudY + 20), $"Damage: {pObj.DamagePercent:F1}% | Stock: {player.StockCount}", 0xFFFFEAA7);
            _render.DrawString(new Point<int>(hudX, hudY + 40), $"State: {player.StateMachine.State}", 0xFFDFE6E9);
        }

        // 操作説明
        _render.DrawString(new Point<int>(30, 100), "[1-4] Set Players Count | [F1] Debug Box | [P1/P2] Keyboard/Pad | [P3/P4] Pad Only", 0xFF888888);
    }
}
