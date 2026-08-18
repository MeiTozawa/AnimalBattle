using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Input;

namespace AnimalBattle.Systems;

/// <summary>
/// R キー押下による戦場・プレイヤー・Boss の 1 フレーム即時リセットを司る ECS System。
/// </summary>
public static class RestartSystem
{
    /// <summary>
    /// リスタートシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System("RestartSystem")
             .Kind(Ecs.PostUpdate)
             .Run((Iter it) =>
             {
                 bool restartJustPressed = InputService.IsPressed(InputAction.Restart);

                 if (restartJustPressed)
                 {
                     PlayerConfig pCfg = ConfigManager.Player;
                     BossConfig bCfg = ConfigManager.Boss;

                     CombatEffectHelper.ClearAllBossBullets(world);

                     world.QueryBuilder()
                          .With<PlayerBulletTag>()
                          .Build()
                          .Each((Entity e) => e.Destruct());

                     world.QueryBuilder()
                          .With<EffectTag>()
                          .Build()
                          .Each((Entity e) => e.Destruct());

                     world.QueryBuilder<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite>()
                          .With<PlayerTag>()
                          .Build()
                          .Each((ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health health, ref DoubleJump doubleJump, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
                          {
                              trans.X = pCfg.SpawnX;
                              trans.Y = pCfg.SpawnY;
                              vel.Vx = 0.0f;
                              vel.Vy = 0.0f;
                              health.Current = pCfg.MaxHp;
                              health.Max = pCfg.MaxHp;
                              health.InvincibleTimer = 0.0f;
                              doubleJump.Reset();
                              playerState.ShootCooldown = 0.0f;
                              col.IsGrounded = true;
                              anim.CurrentClip = "Idle";
                              anim.Timer = 0.0f;
                              sprite.IsVisible = true;
                              sprite.FlipX = false;
                          });

                     world.QueryBuilder<Transform, Health, BossStats, BossStateMachine>()
                          .With<BossTag>()
                          .Build()
                          .Each((ref Transform trans, ref Health health, ref BossStats stats, ref BossStateMachine fsm) =>
                          {
                              trans.X = bCfg.BaseX;
                              trans.Y = bCfg.BaseY;
                              health.Current = bCfg.MaxHp;
                              health.Max = bCfg.MaxHp;
                              health.InvincibleTimer = 0.0f;
                              stats.Phase = 1;
                              stats.AttackTimer1 = 0.5f;
                              stats.AttackTimer2 = 1.0f;
                              stats.AngleOffset = 0.0f;
                              stats.HitFlashTimer = 0.0f;
                              stats.HoverTimer = 0.0f;

                              fsm.CurrentState = BossStateKind.Hovering;
                              fsm.StateTimer = 0.0f;
                              fsm.StateDuration = bCfg.Phase1.HoverDuration;
                              fsm.CurrentSkillIndex = 0;
                              fsm.SubTimer = 0.0f;
                              fsm.AngleOffset = 0.0f;
                              fsm.IsInvulnerable = false;
                          });
                 }
             });
    }
}
