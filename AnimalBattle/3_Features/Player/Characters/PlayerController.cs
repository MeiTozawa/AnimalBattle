using System;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     プレイヤーのローカル入力（キーボード、マウス、ゲームパッド）を解読し、
///     自機エンティティの意図コンポーネント（<see cref="MovementIntent" />, <see cref="WeaponIntent" />）へ注入する入力コントローラー。
///     画面座標系からワールド座標系への照準逆算、およびキャラクター向きの決定を担当します。
/// </summary>
public sealed class PlayerController
{
    private readonly PlayerPawn _pawn;

    /// <summary>
    ///     対象の受控プレイヤー躯体を指定してコントローラーを初期化します。
    /// </summary>
    /// <param name="pawn">受控プレイヤー躯体。</param>
    public PlayerController(PlayerPawn pawn)
    {
        _pawn = pawn;
    }

    /// <summary>
    ///     フレームごとの入力をポーリングし、意図コンポーネントへ送信します。
    /// </summary>
    public void PollAndProcessInput()
    {
        // 水平移動入力値（-1.0f: 左, 1.0f: 右, 0.0f: 静止）
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

        var entity = _pawn.Entity;
        ref readonly var trans = ref entity.Get<Transform>();
        ref readonly var box = ref entity.Get<CollisionBox>();

        // プレイヤー中心（射撃起点）座標
        var playerCenter = new Vec2f(trans.Position.X, trans.Position.Y - box.Size.Y * 0.5f);

        var diff = mouseWorld - playerCenter;
        var distSq = diff.LengthSquared();

        // キャラクター向き（FacingX）：マウスが自機中心より左右にあれば向き決定、微小差分時は前回の向きまたは移動入力を反映
        var currentFacing = entity.Get<MovementIntent>().FacingX;
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

        SubmitInput(moveX, jumpPressed, jumpHolding, shootPressed, dashPressed, facingX, aimDir);
    }

    /// <summary>
    ///     指定された移動・跳躍・射撃・ダッシュ入力をエンティティの意図コンポーネントへ注入します（テスト・AI操作・リプレイ対応）。
    /// </summary>
    /// <param name="moveX">水平移動入力値（-1.0f: 左, 1.0f: 右, 0.0f: 静止）。</param>
    /// <param name="jumpPressed">跳躍キー押下フラグ。</param>
    /// <param name="jumpHolding">跳躍キー保持フラグ。</param>
    /// <param name="shootPressed">射撃キー押下フラグ。</param>
    /// <param name="dashPressed">ダッシュ・回避キー押下フラグ。</param>
    /// <param name="facingX">キャラクター水平指向方向（-1.0f: 左, 1.0f: 右）。</param>
    /// <param name="aimDirection">射撃照準方向ベクトル（省略時は facingX 方向）。</param>
    public void SubmitInput(
        float moveX,
        bool jumpPressed,
        bool jumpHolding,
        bool shootPressed,
        bool dashPressed,
        float facingX,
        Vec2f? aimDirection = null)
    {
        var entity = _pawn.Entity;
        if (!entity.IsAlive()) return;

        ref var moveIntent = ref entity.GetMut<MovementIntent>();
        moveIntent.MoveX = moveX;
        moveIntent.JumpTriggered = jumpPressed;
        moveIntent.JumpHolding = jumpHolding;
        moveIntent.DashTriggered = dashPressed;
        moveIntent.FacingX = facingX;

        ref var weaponIntent = ref entity.GetMut<WeaponIntent>();
        weaponIntent.ShootTriggered = shootPressed;
        weaponIntent.AimDirection = aimDirection ?? new Vec2f(facingX, 0.0f);
    }
}
