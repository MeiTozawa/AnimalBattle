using System;
using AnimalBattle.Common.Components;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     自機のローカル入力：キーボード、マウス、ゲームパッドを解読し、
///     意図コンポーネント：<see cref="MovementIntent" />, <see cref="WeaponIntent" />へ注入する純粋 ECS システム。
///     ゲームオーバー（GameOverTag シングルトン）時は入力を遮断し、意図を安全にゼロクリアします。
/// </summary>
public static class PlayerInputSystem
{
    /// <summary>
    ///     プレイヤー入力システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddPlayerInputSystem(this World world)
    {
        world.System<MovementIntent, WeaponIntent, Transform, CollisionBox>("PlayerInputSystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Kind(Ecs.PreUpdate)
            .Iter(ExecutePlayerInput);

        return world;
    }

    private static void ExecutePlayerInput(
        Iter it,
        Field<MovementIntent> moveIntents,
        Field<WeaponIntent> weaponIntents,
        Field<Transform> transforms,
        Field<CollisionBox> collisionBoxes)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        // ゲームオーバー時（Victory / Defeat）：入力受付を停止し、意図を安全にゼロクリア
        if (world.Has<GameOverTag>())
        {
            for (var i = 0; i < count; i++)
            {
                ref var m = ref moveIntents[i];
                ref var w = ref weaponIntents[i];
                m.MoveX = 0.0f;
                m.JumpTriggered = false;
                m.JumpHolding = false;
                m.DashTriggered = false;
                w.ShootTriggered = false;
            }
            return;
        }

        // 水平移動入力値：-1.0f: 左, 1.0f: 右, 0.0f: 静止
        var moveX = 0.0f;
        if (InputService.IsPressed(InputAction.Left) || InputService.IsHolding(InputAction.Left))
        {
            moveX -= 1.0f;
        }

        if (InputService.IsPressed(InputAction.Right) || InputService.IsHolding(InputAction.Right))
        {
            moveX += 1.0f;
        }

        // 跳躍・射撃・回避キーの状態取得
        var jumpPressed = InputService.IsPressed(InputAction.Jump);
        var jumpHolding = InputService.IsHolding(InputAction.Jump);
        var shootPressed = InputService.IsPressed(InputAction.Attack) || InputService.IsHolding(InputAction.Attack);
        var dashPressed = InputService.IsPressed(InputAction.Skill);

        // マウス画面座標からカメラ逆オフセットでワールド座標を逆算
        var mouseScreen = InputService.GetMousePosition();
        var mouseWorld = mouseScreen.ToVec2f() - (Vec2f)CameraService.RenderOffset;

        for (var i = 0; i < count; i++)
        {
            ref var moveIntent = ref moveIntents[i];
            ref var weaponIntent = ref weaponIntents[i];
            ref readonly var trans = ref transforms[i];
            ref readonly var box = ref collisionBoxes[i];

            // プレイヤー中心：射撃起点座標
            var playerCenter = new Vec2f(trans.Position.X, trans.Position.Y - box.Size.Y * 0.5f);
            var diff = mouseWorld - playerCenter;
            var distSq = diff.LengthSquared();

            // キャラクター向き：FacingX：マウスが自機中心より左右にあれば向き決定、微小差分時は前回の向きまたは移動入力を反映
            var currentFacing = moveIntent.FacingX;
            if (currentFacing == 0.0f) currentFacing = 1.0f;

            var facingX = currentFacing;
            if (MathF.Abs(diff.X) > 0.5f)
            {
                facingX = diff.X < 0.0f ? -1.0f : 1.0f;
            }
            else if (MathF.Abs(moveX) > 0.01f)
            {
                facingX = MathF.Sign(moveX);
            }

            var aimDir = distSq > 0.0001f ? diff.Normalized() : new Vec2f(facingX, 0.0f);

            moveIntent.MoveX = moveX;
            moveIntent.JumpTriggered = jumpPressed;
            moveIntent.JumpHolding = jumpHolding;
            moveIntent.DashTriggered = dashPressed;
            moveIntent.FacingX = facingX;

            weaponIntent.ShootTriggered = shootPressed;
            weaponIntent.AimDirection = aimDir;
        }
    }
}
