using AnimalBattle.Components;
using AnimalBattle.Configs;

namespace AnimalBattle.Characters;

/// <summary>
///     自機プレイヤーコントローラー（OOP 領域モデル）。
///     ユーザーハードウェア入力を収集し、意図コンポーネント（CharacterMovementIntent / CharacterWeaponIntent）に変換して Entity へ適用します。
/// </summary>
public sealed class PlayerController
{
    private readonly ICharacter _player;

    /// <summary>
    ///     制御対象の自機プレイヤー（Pawn）を指定してコントローラーを初期化します。
    /// </summary>
    /// <param name="player">制御対象のプレイヤーインスタンス。</param>
    public PlayerController(ICharacter player)
    {
        System.Diagnostics.Debug.Assert(player != null, "Player インスタンスは null であってはなりません。");
        _player = player!;
    }

    /// <summary>
    ///     グローバル入力サービスからハードウェア入力をポーリングし、エンティティへ意図を反映します。
    /// </summary>
    public void PollAndProcessInput()
    {
        if (!_player.IsAlive) return;

        var isLeft = InputService.IsHolding(InputAction.Left);
        var isRight = InputService.IsHolding(InputAction.Right);
        var moveX = 0.0f;
        if (isLeft) moveX = -1.0f;
        else if (isRight) moveX = 1.0f;

        var jumpPressed = InputService.IsPressed(InputAction.Jump);
        var jumpHolding = InputService.IsHolding(InputAction.Jump);
        var shootPressed = InputService.IsPressed(InputAction.Attack) || InputService.IsHolding(InputAction.Attack);

        var isUp = InputService.IsHolding(InputAction.Up);
        var isDown = InputService.IsHolding(InputAction.Down);
        var inputX = (isRight ? 1.0f : 0.0f) - (isLeft ? 1.0f : 0.0f);
        var inputY = (isDown ? 1.0f : 0.0f) - (isUp ? 1.0f : 0.0f);

        float dirX, dirY;
        if (Math.Abs(inputX) > 0.001f || Math.Abs(inputY) > 0.001f)
        {
            var len = MathF.Sqrt(inputX * inputX + inputY * inputY);
            dirX = inputX / len;
            dirY = inputY / len;
        }
        else
        {
            dirX = _player.Entity.Get<Sprite>().FlipX ? -1.0f : 1.0f;
            dirY = 0.0f;
        }

        SubmitInput(moveX, jumpPressed, jumpHolding, shootPressed, dirX, dirY);
    }

    /// <summary>
    ///     指定された移動・跳躍・射撃入力をエンティティの意図コンポーネントへ注入します（テスト・AI操作・リプレイ対応）。
    /// </summary>
    /// <param name="moveX">水平移動入力値（-1.0f: 左, 1.0f: 右, 0.0f: 静止）。</param>
    /// <param name="jumpPressed">跳躍キー押下フラグ。</param>
    /// <param name="jumpHolding">跳躍キー保持フラグ。</param>
    /// <param name="shootPressed">射撃キー押下フラグ。</param>
    /// <param name="aimDirX">射撃水平瞄準ベクトル（省略時はスプライト向き）。</param>
    /// <param name="aimDirY">射撃垂直瞄準ベクトル（省略時は 0.0f）。</param>
    public void SubmitInput(
        float moveX,
        bool jumpPressed,
        bool jumpHolding,
        bool shootPressed,
        float? aimDirX = null,
        float? aimDirY = null)
    {
        var entity = _player.Entity;
        if (!entity.IsAlive()) return;

        ref var moveIntent = ref entity.GetMut<CharacterMovementIntent>();
        moveIntent.MoveX = moveX;
        moveIntent.JumpTriggered = jumpPressed;
        moveIntent.JumpHolding = jumpHolding;

        ref var weaponIntent = ref entity.GetMut<CharacterWeaponIntent>();
        weaponIntent.ShootTriggered = shootPressed;
        weaponIntent.AimDirectionX = aimDirX ?? (entity.Get<Sprite>().FlipX ? -1.0f : 1.0f);
        weaponIntent.AimDirectionY = aimDirY ?? 0.0f;

        // 移動入力に応じた描画スプライトの左右反転同期
        ref var spr = ref entity.GetMut<Sprite>();
        if (moveX < -0.01f) spr.FlipX = true;
        else if (moveX > 0.01f) spr.FlipX = false;
    }
}
