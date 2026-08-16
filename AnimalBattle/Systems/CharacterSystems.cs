using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;
/// <summary>
/// キャラクター入力検知、物理運動、およびアニメーション状態遷移を統括する Flecs.NET ECS システム群。
/// </summary>
public static class CharacterSystems
{
    /// <summary>
    /// キーボードおよびジョイパッド入力を検知し、PlayerTag を持つ各エンティティの <see cref="Input"/> コンポーネントへ書き込みます。
    /// </summary>
    public static readonly Action<World> InputSystem = (world) =>
    {
        world.QueryBuilder<PlayerId, Input>()
             .Build()
             .Each((ref PlayerId pid, ref Input input) =>
             {
                 input.PrevAttack = input.Attack;

                 if (pid.Id == 1)
                 {
                     bool keyboardLeft = GameEngine.CheckHitKey(0x1E) != 0;
                     bool keyboardRight = GameEngine.CheckHitKey(0x20) != 0;
                     bool keyboardJump = GameEngine.CheckHitKey(0x11) != 0 || GameEngine.CheckHitKey(0x39) != 0;
                     bool keyboardAttack = GameEngine.CheckHitKey(0x24) != 0;

                     int pad = GameEngine.GetJoypadInputState(0);
                     input.MoveLeft = keyboardLeft || (pad & 0x0001) != 0;
                     input.MoveRight = keyboardRight || (pad & 0x0002) != 0;
                     input.Jump = keyboardJump || (pad & 0x0010) != 0;
                     input.Attack = keyboardAttack || (pad & 0x0020) != 0;
                 }
                 else if (pid.Id == 2)
                 {
                     bool keyboardLeft = GameEngine.CheckHitKey(0xCB) != 0;
                     bool keyboardRight = GameEngine.CheckHitKey(0xCD) != 0;
                     bool keyboardJump = GameEngine.CheckHitKey(0xC8) != 0;
                     bool keyboardAttack = GameEngine.CheckHitKey(0x26) != 0;

                     int pad = GameEngine.GetJoypadInputState(1);
                     input.MoveLeft = keyboardLeft || (pad & 0x0001) != 0;
                     input.MoveRight = keyboardRight || (pad & 0x0002) != 0;
                     input.Jump = keyboardJump || (pad & 0x0010) != 0;
                     input.Attack = keyboardAttack || (pad & 0x0020) != 0;
                 }
                 else
                 {
                     int padIndex = pid.Id - 1;
                     int pad = GameEngine.GetJoypadInputState(padIndex);
                     input.MoveLeft = (pad & 0x0001) != 0;
                     input.MoveRight = (pad & 0x0002) != 0;
                     input.Jump = (pad & 0x0010) != 0;
                     input.Attack = (pad & 0x0020) != 0;
                 }

                 input.AttackJustPressed = input.Attack && !input.PrevAttack;
             });
    };

    /// <summary>
    /// 入力状態および接地フラグに基づき、キャラクターの水平移動速度・ジャンプ初速・攻撃クールダウンを計算します。
    /// </summary>
    public static readonly Action<World> MovementSystem = (world) =>
    {
        world.QueryBuilder<Input, Velocity, CollisionBox, Animation, Sprite, CharacterStats>()
             .Build()
             .Each((ref Input input, ref Velocity vel, ref CollisionBox col, ref Animation anim, ref Sprite sprite, ref CharacterStats stats) =>
             {
                 if (stats.AttackCooldown > 0.0f)
                 {
                     stats.AttackCooldown -= 0.016f;
                 }

                 if (input.MoveLeft)
                 {
                     vel.Vx = -5.0f;
                     sprite.FlipX = true;
                 }
                 else if (input.MoveRight)
                 {
                     vel.Vx = 5.0f;
                     sprite.FlipX = false;
                 }
                 else
                 {
                     vel.Vx = 0.0f;
                 }

                 if (input.Jump && col.IsGrounded)
                 {
                     vel.Vy = -11.5f;
                     col.IsGrounded = false;
                 }

                 if (input.AttackJustPressed && stats.AttackCooldown <= 0.0f)
                 {
                     anim.CurrentClip = "Attack";
                     anim.Timer = 0.0f;
                     stats.AttackCooldown = 0.35f;
                 }
             });
    };

    /// <summary>
    /// 運動状態（接地・ジャンプ・移動・攻撃）に応じてアニメーションクリップを切り替え、スプライト UV 切り出し矩形を更新します。
    /// </summary>
    public static readonly Action<World> AnimationSystem = (world) =>
    {
        world.QueryBuilder<Velocity, CollisionBox, Animation, Sprite>()
             .Build()
             .Each((ref Velocity vel, ref CollisionBox col, ref Animation anim, ref Sprite sprite) =>
             {
                 anim.Timer += 0.016f;

                 if (!string.Equals(anim.CurrentClip, "Attack", StringComparison.OrdinalIgnoreCase))
                 {
                     if (!col.IsGrounded)
                     {
                         anim.CurrentClip = "Jump";
                     }
                     else if (Math.Abs(vel.Vx) > 0.1f)
                     {
                         anim.CurrentClip = "Run";
                     }
                     else
                     {
                         anim.CurrentClip = "Idle";
                     }
                 }

                 AnimationConfig? charConfig = AnimationManager.GetCharacterConfig(CharacterType.CatSpriteSheet);
                 if (charConfig != null)
                 {
                     sprite.SrcRect = AnimationManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out bool isFinished);
                     if (isFinished && string.Equals(anim.CurrentClip, "Attack", StringComparison.OrdinalIgnoreCase))
                     {
                         anim.CurrentClip = "Idle";
                         anim.Timer = 0.0f;
                     }
                 }
             });
    };
}
