using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     自機プレイヤーコントローラー（入力解釈器）。
///     ユーザーハードウェア入力を収集し、意図コンポーネント（CharacterMovementIntent / CharacterWeaponIntent）に変換して PlayerPawn へ注入します。
/// </summary>
public sealed class PlayerController
{
    private readonly PlayerPawn _pawn;

    /// <summary>
    ///     制御対象の自機躯体（PlayerPawn）を指定してコントローラーを初期化します。
    /// </summary>
    /// <param name="pawn">制御対象の PlayerPawn インスタンス。</param>
    public PlayerController(PlayerPawn pawn)
    {
        System.Diagnostics.Debug.Assert(pawn != null, "PlayerPawn インスタンスは null であってはなりません。");
        _pawn = pawn;
    }

    /// <summary>制御対象の PlayerPawn を取得します。</summary>
    public PlayerPawn Pawn => _pawn;

    /// <summary>
    ///     グローバル入力サービスからハードウェア入力をポーリングし、エンティティへ意図を反映します。
    /// </summary>
    public void PollAndProcessInput()
    {
        if (!_pawn.IsAlive) return;

        var isLeft = InputService.IsHolding(InputAction.Left);
        var isRight = InputService.IsHolding(InputAction.Right);
        var moveX = 0.0f;
        if (isLeft) moveX = -1.0f;
        else if (isRight) moveX = 1.0f;

        var jumpPressed = InputService.IsPressed(InputAction.Jump);
        var jumpHolding = InputService.IsHolding(InputAction.Jump);
        var shootPressed = InputService.IsPressed(InputAction.Attack) || InputService.IsHolding(InputAction.Attack);
        var dashPressed = InputService.IsPressed(InputAction.Skill);

        // マウス画面座標からカメラ逆オフセットでワールド座標を逆算
        var mouseScreen = InputService.GetMousePosition();
        var mouseWorldX = mouseScreen.X - CameraService.RenderOffsetX;
        var mouseWorldY = mouseScreen.Y - CameraService.RenderOffsetY;

        var entity = _pawn.Entity;
        ref readonly var trans = ref entity.Get<Transform>();
        ref readonly var box = ref entity.Get<CollisionBox>();

        // プレイヤー中心（射撃起点）座標
        var playerCenterX = trans.X;
        var playerCenterY = trans.Y - box.Height * 0.5f;

        var diffX = mouseWorldX - playerCenterX;
        var diffY = mouseWorldY - playerCenterY;
        var distSq = diffX * diffX + diffY * diffY;

        float dirX, dirY;
        if (distSq > 0.0001f)
        {
            var invDist = 1.0f / MathF.Sqrt(distSq);
            dirX = diffX * invDist;
            dirY = diffY * invDist;
        }
        else
        {
            dirX = _pawn.FlipX ? -1.0f : 1.0f;
            dirY = 0.0f;
        }

        // キャラクター向き（FlipX）：マウスが自機中心より左にあれば反転、右にあれば正面
        if (MathF.Abs(diffX) > 0.5f)
        {
            _pawn.FlipX = diffX < 0.0f;
        }

        SubmitInput(moveX, jumpPressed, jumpHolding, shootPressed, dashPressed, dirX, dirY);
    }

    /// <summary>
    ///     指定された移動・跳躍・射撃・ダッシュ入力をエンティティの意図コンポーネントへ注入します（テスト・AI操作・リプレイ対応）。
    /// </summary>
    /// <param name="moveX">水平移動入力値（-1.0f: 左, 1.0f: 右, 0.0f: 静止）。</param>
    /// <param name="jumpPressed">跳躍キー押下フラグ。</param>
    /// <param name="jumpHolding">跳躍キー保持フラグ。</param>
    /// <param name="shootPressed">射撃キー押下フラグ。</param>
    /// <param name="dashPressed">ダッシュ・回避キー押下フラグ。</param>
    /// <param name="aimDirX">射撃水平瞄準ベクトル（省略時はスプライト向き）。</param>
    /// <param name="aimDirY">射撃垂直瞄準ベクトル（省略時は 0.0f）。</param>
    private void SubmitInput(
        float moveX,
        bool jumpPressed,
        bool jumpHolding,
        bool shootPressed,
        bool dashPressed,
        float? aimDirX = null,
        float? aimDirY = null)
    {
        var entity = _pawn.Entity;
        if (!entity.IsAlive()) return;

        ref var moveIntent = ref entity.GetMut<MovementIntent>();
        moveIntent.MoveX = moveX;
        moveIntent.JumpTriggered = jumpPressed;
        moveIntent.JumpHolding = jumpHolding;
        moveIntent.DashTriggered = dashPressed;

        ref var weaponIntent = ref entity.GetMut<WeaponIntent>();
        weaponIntent.ShootTriggered = shootPressed;
        weaponIntent.AimDirectionX = aimDirX ?? (_pawn.FlipX ? -1.0f : 1.0f);
        weaponIntent.AimDirectionY = aimDirY ?? 0.0f;

        if (!aimDirX.HasValue)
        {
            _pawn.FlipX = moveX switch {
                // 移動入力に応じた描画スプライトの左右反転同期（aimDirX 未指定時のみ）
                < -0.01f => true,
                > 0.01f => false,
                _ => _pawn.FlipX
            };
        }
    }
}
