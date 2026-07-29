using System;
using AnimalBattle.Animations;
using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Inputs;

namespace AnimalBattle.Scenes;

public partial class BattleScene
{
    /// <summary>
    /// 人数変更キー (1-4)、マップ切替、バトル更新、アニメーション更新、およびカメラ更新を行います。
    /// </summary>
    public bool Update(float deltaTime)
    {
        _input.Update();

        if (_input.IsPressed(InputAction.ToggleMenu))
        {
            return false;
        }

        if (_input.IsPressed(InputAction.DebugToggle))
        {
            _showDebugBoxes = !_showDebugBoxes;
            Console.WriteLine($"[Debug 表示] {(_showDebugBoxes ? "ON" : "OFF")}");
        }

        // 数字キー 1..4 による参戦人数の動的切り替え
        int requestedPlayerCount = _input.GetRequestedPlayerCountSwitch();
        if (requestedPlayerCount >= 1 && requestedPlayerCount <= 4 && requestedPlayerCount != _battleManager.Players.Count)
        {
            _battleManager.Initialize(_physicsEngine, _stageManager.CurrentMap, requestedPlayerCount);
            _animationManager.EnsurePlayerCount(requestedPlayerCount);
            Console.WriteLine($"[人数切り替え] プレイヤー人数を {requestedPlayerCount} 人に変更しました！");
        }

        _battleManager.Update(_physicsEngine, _input, _stageManager.CurrentMap, deltaTime);

        var players = _battleManager.Players;
        int count = players.Count;
        if (count == 0) return true;

        Span<NativePhysicsObject> pObjs = stackalloc NativePhysicsObject[count];
        for (int i = 0; i < count; i++)
        {
            _physicsEngine.GetObjectState(players[i].PhysicsObjectId, out pObjs[i]);
        }

        // アニメーション状態の判定と更新
        if (_animationManager.HasCatSprite)
        {
            static CharacterAnimationType UpdateStableAnim(CharacterAnimationType prevAnim, CharacterStateMachine sm, ref NativePhysicsObject pObj)
            {
                if (sm.State == CharacterState.Hitstun) return CharacterAnimationType.Hurt;
                if (sm.State == CharacterState.Attacking) return CharacterAnimationType.Attack;
                if (sm.State == CharacterState.Airborne || sm.State == CharacterState.FastFalling)
                {
                    return pObj.Vy < 0.0f ? CharacterAnimationType.Jump : CharacterAnimationType.Fall;
                }

                float speedX = Math.Abs(pObj.Vx);
                if (speedX > 10.0f) return CharacterAnimationType.Run;
                return CharacterAnimationType.Idle;
            }

            for (int i = 0; i < count; i++)
            {
                var player = players[i];
                if (player.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.35f })
                {
                    _animationManager.PlayEffectAnimation(player.PlayerIndex, EffectAnimationType.Slash);
                }

                _playerCurrentAnims[i] = UpdateStableAnim(_playerCurrentAnims[i], player.StateMachine, ref pObjs[i]);
                _animationManager.PlayCharacterAnimation(player.PlayerIndex, _playerCurrentAnims[i]);
            }

            _animationManager.Update(deltaTime);
        }

        // カメラ追従ターゲットの動的更新
        Span<Point<float>> targets = stackalloc Point<float>[count];
        for (int i = 0; i < count; i++)
        {
            targets[i] = new Point<float>(pObjs[i].X, pObjs[i].Y - pObjs[i].Height / 2.0f);
        }

        _camera.Update(deltaTime, targets, 1280.0f, 720.0f);

        return true;
    }
}
