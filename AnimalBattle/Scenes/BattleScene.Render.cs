using System;
using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Stages;

namespace AnimalBattle.Scenes;

public partial class BattleScene
{
    /// <summary>
    /// バトルシーンのワールド、キャラクター、エフェクト、および HUD UI を描画します。
    /// </summary>
    public void Render()
    {
        MapData currentMap = _stageManager.CurrentMap;
        var players = _battleManager.Players;
        int count = players.Count;
        if (count == 0) return;

        Span<NativePhysicsObject> pObjs = stackalloc NativePhysicsObject[count];
        for (int i = 0; i < count; i++)
        {
            _physicsEngine.GetObjectState(players[i].PhysicsObjectId, out pObjs[i]);
        }

        var (camX, camY, camScale) = _camera.GetState();

        _render.SetCameraEnabled(true);
        _render.SetCameraState(camX, camY, camScale);

        // 背景描画
        _render.DrawBox(new Rect<int>(0, 0, 1280, 720), currentMap.BgColor, true);

        // プラットフォーム描画
        foreach (var plat in currentMap.Platforms)
        {
            int px1 = (int)plat.X1;
            int py1 = (int)plat.Y1;
            int px2 = (int)plat.X2;
            int py2 = (int)plat.Y2;

            _render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.FillColor, true);

            if (plat.Type == PlatformType.SemiSolid)
            {
                _render.DrawBox(new Rect<int>(px1, py1, px2, py1 + 2), 0xFFFFFFFF, true);
            }
        }

        float spriteScale = 2.5f;
        float spriteHalfHeight = (32.0f * spriteScale) / 2.0f;

        // キャラクターおよびスプライト描画
        for (int i = 0; i < count; i++)
        {
            var player = players[i];
            ref readonly var pObj = ref pObjs[i];

            int px1 = (int)(pObj.X - pObj.Width / 2.0f);
            int py1 = (int)(pObj.Y - pObj.Height);
            int px2 = (int)(pObj.X + pObj.Width / 2.0f);
            int py2 = (int)pObj.Y;
            uint pColor = (player.StateMachine.State == CharacterState.Hitstun) ?
                (((int)(pObj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
                ((player.StateMachine.State == CharacterState.Grounded) ? player.MainColor : player.OutlineColor);

            if (_animationManager.HasCatSprite)
            {
                var catPos = new Point<int>((int)MathF.Round(pObj.X), (int)MathF.Round(pObj.Y - spriteHalfHeight));
                _animationManager.DrawCharacter(_render, player.PlayerIndex, catPos, facingLeft: player.FacingLeft, scale: spriteScale);
            }
            else
            {
                _render.DrawBox(new Rect<int>(px1, py1, px2, py2), pColor, true);
            }

            // 攻撃エフェクト
            if (_animationManager.HasEffectSprite && player.StateMachine.State == CharacterState.Attacking)
            {
                var hitBounds = player.Hitbox.GetWorldBounds(pObj.X, pObj.Y, player.FacingLeft);
                var fxPos = new Point<int>((int)MathF.Round((hitBounds.X1 + hitBounds.X2) / 2.0f), (int)MathF.Round((hitBounds.Y1 + hitBounds.Y2) / 2.0f));
                _animationManager.DrawEffect(_render, player.PlayerIndex, fxPos, facingLeft: player.FacingLeft, scale: 2.0f);
            }

            // Debug 判定枠表示
            if (_showDebugBoxes)
            {
                var hurtBounds = player.Hurtbox.GetWorldBounds(pObj.X, pObj.Y, player.FacingLeft);
                _render.DrawBox(new Rect<int>((int)hurtBounds.X1, (int)hurtBounds.Y1, (int)hurtBounds.X2, (int)hurtBounds.Y2), 0xFF00FF00, false);

                if (player.Hitbox.IsActive)
                {
                    var hitBounds = player.Hitbox.GetWorldBounds(pObj.X, pObj.Y, player.FacingLeft);
                    _render.DrawBox(new Rect<int>((int)hitBounds.X1, (int)hitBounds.Y1, (int)hitBounds.X2, (int)hitBounds.Y2), 0xFFFF0000, true);
                    _render.DrawBox(new Rect<int>((int)hitBounds.X1, (int)hitBounds.Y1, (int)hitBounds.X2, (int)hitBounds.Y2), 0xFFFFFFFF, false);
                }
            }
        }

        // HUD UI の描画を別 partial メソッドへ集約呼び出し
        RenderHud(currentMap, players, pObjs, camX, camY, camScale);
    }
}
