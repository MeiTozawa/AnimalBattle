using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// BossAiRuntime が発行した Movement / Attack / Effect の各コマンドを
/// Flecs.NET ECS ワールド上の Entity（Transform, Velocity, 弾幕, 近接判定, 視覚効果）へ反映する実行器クラス。
/// </summary>
public static class BossCommandExecutor
{
    /// <summary>
    /// Boss の移動コマンドおよび物理・地形判定を ECS Entity へ適用します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="entity">対象の Boss Entity</param>
    /// <param name="slot">Boss の runtime slot</param>
    /// <param name="dt">前フレームからの経過時間（秒）</param>
    /// <param name="platformQuery">プラットフォーム足場クエリ</param>
    public static void ExecuteMovement(
        World world,
        Entity entity,
        BossRuntimeSlot slot,
        float dt,
        Query<Transform, CollisionBox> platformQuery)
    {
        if (!entity.Has<Transform>() || !entity.Has<Velocity>())
        {
            return;
        }

        ref Transform trans = ref entity.GetMut<Transform>();
        ref Velocity vel = ref entity.GetMut<Velocity>();

        if (slot.BossType == BossType.Boss1)
        {
            ExecuteBoss1Movement(slot, dt, ref trans, ref vel);
        }
        else if (slot.BossType == BossType.Boss2)
        {
            ExecuteBoss2Movement(slot, dt, ref trans, ref vel, entity, platformQuery);
        }
    }

    /// <summary>
    /// Boss の攻撃コマンド（弾幕発射、近接 Hitbox 生成、弾幕消去）を ECS ワールドへ生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="entity">対象の Boss Entity</param>
    /// <param name="slot">Boss の runtime slot</param>
    public static void ExecuteAttack(
        World world,
        Entity entity,
        BossRuntimeSlot slot)
    {
        if (slot.AttackCommands.Count == 0 || !entity.Has<Transform>())
        {
            return;
        }

        PatternConfig patternCfg = ConfigManager.Pattern;
        Boss2Config b2Cfg = ConfigManager.Boss2;
        ref readonly Transform trans = ref entity.Get<Transform>();
        ref readonly Velocity vel = ref entity.Get<Velocity>();

        for (int i = 0; i < slot.AttackCommands.Count; i++)
        {
            AttackCommand cmd = slot.AttackCommands[i];
            switch (cmd.Kind)
            {
                case AttackCommandKind.ClearBossBullets:
                    CombatEffectHelper.ClearAllBossBullets(world);
                    break;

                case AttackCommandKind.EmitBullet:
                    ExecuteEmitBullet(world, in trans, in cmd, patternCfg);
                    break;

                case AttackCommandKind.CreateHitbox:
                    ExecuteCreateHitbox(world, entity, in trans, in vel, in cmd, patternCfg, b2Cfg, slot);
                    break;
            }
        }
    }

    /// <summary>
    /// Boss の演出コマンド（予兆スパーク、衝撃波、死亡爆発等）を ECS ワールドへ生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="entity">対象の Boss Entity</param>
    /// <param name="slot">Boss の runtime slot</param>
    public static void ExecuteEffects(
        World world,
        Entity entity,
        BossRuntimeSlot slot)
    {
        if (!entity.Has<Transform>())
        {
            return;
        }
    }

    private static void ExecuteBoss1Movement(
        BossRuntimeSlot slot,
        float dt,
        ref Transform trans,
        ref Velocity vel)
    {
        bool hasMoveTo = false;
        for (int i = 0; i < slot.MovementCommands.Count; i++)
        {
            MovementCommand cmd = slot.MovementCommands[i];
            switch (cmd.Kind)
            {
                case MovementCommandKind.MoveTo:
                    trans.X = cmd.X;
                    trans.Y = cmd.Y;
                    hasMoveTo = true;
                    break;
                case MovementCommandKind.SetVelocity:
                    vel.Vx = cmd.X;
                    vel.Vy = cmd.Y;
                    break;
                case MovementCommandKind.Stop:
                    vel.Vx = 0.0f;
                    vel.Vy = 0.0f;
                    break;
            }
        }

        if (!hasMoveTo && (vel.Vx != 0.0f || vel.Vy != 0.0f))
        {
            trans.X += vel.Vx * dt;
            trans.Y += vel.Vy * dt;
        }
    }

    private static void ExecuteBoss2Movement(
        BossRuntimeSlot slot,
        float dt,
        ref Transform trans,
        ref Velocity vel,
        Entity entity,
        Query<Transform, CollisionBox> platformQuery)
    {
        Boss2Config b2Cfg = ConfigManager.Boss2;
        ArenaConfig arenaCfg = ConfigManager.Arena;

        bool gravityHandled = false;
        bool hasMoveTo = false;

        for (int i = 0; i < slot.MovementCommands.Count; i++)
        {
            MovementCommand cmd = slot.MovementCommands[i];
            switch (cmd.Kind)
            {
                case MovementCommandKind.SetVelocity:
                    vel.Vx = cmd.X;
                    if (cmd.Y != 0.0f)
                    {
                        vel.Vy = cmd.Y;
                    }
                break;
                case MovementCommandKind.MoveTo:
                    trans.X = cmd.X;
                    trans.Y = cmd.Y;
                    hasMoveTo = true;
                    break;
                case MovementCommandKind.ApplyGravity:
                    float gVy = vel.Vy;
                    PhysicsService.ApplyGravity(ref gVy, cmd.X, cmd.Y, dt);
                    vel.Vy = gVy;
                    gravityHandled = true;
                    break;
                case MovementCommandKind.Stop:
                    vel.Vx = 0.0f;
                    break;
            }
        }

        if (hasMoveTo)
        {
            if (entity.Has<CollisionBox>())
            {
                ref CollisionBox col = ref entity.GetMut<CollisionBox>();
                col.IsGrounded = false;
            }
            return;
        }

        if (!gravityHandled)
        {
            float curVy = vel.Vy;
            PhysicsService.ApplyGravity(ref curVy, b2Cfg.Gravity, 850.0f, dt);
            vel.Vy = curVy;
        }

        float nextX = trans.X + vel.Vx * dt;
        float nextY = trans.Y + vel.Vy * dt;
        nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

        float currentY = trans.Y;

        float colWidth = entity.Has<CollisionBox>() ? entity.Get<CollisionBox>().Width : b2Cfg.CollisionWidth;
        float colHeight = entity.Has<CollisionBox>() ? entity.Get<CollisionBox>().Height : b2Cfg.CollisionHeight;
        Box2D bossBox = new(nextX, nextY, colWidth, colHeight);

        bool hitPlatform = false;
        float platformGroundY = float.MaxValue;
        float platformVy = 0.0f;

        bool ignorePlatforms = slot.AttackData.Skill == BossSkill.LeapSmash;
        if (!ignorePlatforms)
        {
            platformQuery.Each((ref platTrans, ref platCol) =>
            {
                Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
                if (PhysicsService.CheckPlatformGround(
                    currentY,
                    nextY,
                    bossBox,
                    platformBox,
                    out float groundY,
                    out float pVy))
                {
                    if (!hitPlatform || groundY < platformGroundY)
                    {
                        hitPlatform = true;
                        platformGroundY = groundY;
                        platformVy = pVy;
                    }
                }
            });
        }

        bool isGrounded = hitPlatform;
        float adjustedPosY = hitPlatform ? platformGroundY : nextY;
        float adjustedVy = hitPlatform ? platformVy : vel.Vy;

        float floorLimitY = 560.0f - colHeight / 2.0f;
        if (adjustedPosY >= floorLimitY)
        {
            isGrounded = true;
            adjustedPosY = floorLimitY;
            adjustedVy = 0.0f;
        }

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;

        if (entity.Has<CollisionBox>())
        {
            ref CollisionBox col = ref entity.GetMut<CollisionBox>();
            col.IsGrounded = isGrounded;
        }

        if (entity.Has<Sprite>())
        {
            ref Sprite sprite = ref entity.GetMut<Sprite>();
            sprite.FlipX = !slot.MovementData.FacingRight;
        }
    }

    private static void ExecuteEmitBullet(
        World world,
        in Transform trans,
        in AttackCommand cmd,
        PatternConfig patternCfg)
    {
        if (cmd.Skill == BossSkill.RainDrop)
        {
            BulletType bType = PatternConfig.ParseBulletType(patternCfg.RainDrop.BulletType, BulletType.Cyan);
            CombatEffectHelper.SpawnBossSpecialBullet(
                world,
                cmd.X,
                0.0f,
                0.0f,
                cmd.Y,
                patternCfg.RainDrop.BulletRadius,
                bType,
                patternCfg.RainDrop.FillColor,
                patternCfg.RainDrop.OutlineColor);
            return;
        }

        if (cmd.Skill == BossSkill.EarthquakeRoar)
        {
            Random rand = Random.Shared;
            float rx = rand.Next((int)patternCfg.EarthquakeRoar.MinX, (int)patternCfg.EarthquakeRoar.MaxX);
            CombatEffectHelper.SpawnBossSpecialBullet(
                world,
                rx,
                0.0f,
                0.0f,
                cmd.Y,
                patternCfg.EarthquakeRoar.RockRadius,
                BulletType.Normal,
                patternCfg.EarthquakeRoar.RockFillColor,
                patternCfg.EarthquakeRoar.RockOutlineColor);
            return;
        }

        float radius;
        Color fill;
        Color outline;
        BulletType type = BulletType.Normal;

        if (cmd.Skill == BossSkill.Aimed3Shot)
        {
            radius = patternCfg.Aimed3Shot.BulletRadius;
            fill = patternCfg.Aimed3Shot.FillColor;
            outline = patternCfg.Aimed3Shot.OutlineColor;
            type = PatternConfig.ParseBulletType(patternCfg.Aimed3Shot.BulletType, BulletType.Cyan);
        }
        else if (cmd.Skill == BossSkill.RingBurst)
        {
            radius = patternCfg.RingBurst.BulletRadius;
            fill = patternCfg.RingBurst.FillColor;
            outline = patternCfg.RingBurst.OutlineColor;
            type = PatternConfig.ParseBulletType(patternCfg.RingBurst.BulletType, BulletType.Magenta);
        }
        else if (cmd.Skill == BossSkill.DoubleSpiral)
        {
            radius = patternCfg.DoubleSpiral.BulletRadius;
            fill = patternCfg.DoubleSpiral.FillColor1;
            outline = patternCfg.DoubleSpiral.OutlineColor1;
            type = PatternConfig.ParseBulletType(patternCfg.DoubleSpiral.BulletType, BulletType.Yellow);
        }
        else if (cmd.Skill == BossSkill.CrossChaos)
        {
            radius = patternCfg.CrossChaos.BulletRadius;
            fill = patternCfg.CrossChaos.FillColor;
            outline = patternCfg.CrossChaos.OutlineColor;
            type = PatternConfig.ParseBulletType(patternCfg.CrossChaos.BulletType, BulletType.Magenta);
        }
        else if (cmd.Skill == BossSkill.FastSniper)
        {
            radius = patternCfg.FastSniper.BulletRadius;
            fill = patternCfg.FastSniper.FillColor;
            outline = patternCfg.FastSniper.OutlineColor;
            type = PatternConfig.ParseBulletType(patternCfg.FastSniper.BulletType, BulletType.Black);
        }
        else
        {
            radius = 6.0f;
            fill = ColorPalette.RedBright;
            outline = ColorPalette.White;
        }

        CombatEffectHelper.SpawnBossSpecialBullet(world, trans.X, trans.Y, cmd.X, cmd.Y, radius, type, fill, outline);
    }

    private static void ExecuteCreateHitbox(
        World world,
        Entity entity,
        in Transform trans,
        in Velocity vel,
        in AttackCommand cmd,
        PatternConfig patternCfg,
        Boss2Config b2Cfg,
        BossRuntimeSlot slot)
    {
        float forwardDir = slot.MovementData.FacingRight ? 1.0f : -1.0f;
        SlashDirection dir = slot.MovementData.FacingRight ? SlashDirection.Right : SlashDirection.Left;


        if (cmd.Skill == BossSkill.HeavySlash)
        {
            float offsetX = cmd.X != 0.0f ? cmd.X : b2Cfg.SlashSpawnOffsetX;
            float offsetY = cmd.Y != 0.0f ? cmd.Y : b2Cfg.SlashSpawnOffsetY;
            float hitX = trans.X + forwardDir * offsetX;
            float hitY = trans.Y + offsetY;
            CombatEffectHelper.SpawnMeleeSlashHitbox(
                world,
                hitX,
                hitY,
                vel.Vx,
                0.0f,
                patternCfg.HeavySlash.SlashRangeX,
                patternCfg.HeavySlash.SlashRangeY,
                patternCfg.HeavySlash.SlashDamage,
                0.28f,
                patternCfg.HeavySlash.SlashColor,
                dir);
        }
        else if (cmd.Skill == BossSkill.DoubleCleave)
        {
            float offsetX = cmd.X != 0.0f ? cmd.X : b2Cfg.SlashSpawnOffsetX;
            float offsetY = cmd.Y != 0.0f ? cmd.Y : b2Cfg.SlashSpawnOffsetY;
            float hitX = trans.X + forwardDir * offsetX;
            float hitY = trans.Y + offsetY;
            CombatEffectHelper.SpawnMeleeSlashHitbox(
                world,
                hitX,
                hitY,
                vel.Vx,
                0.0f,
                patternCfg.DoubleCleave.SlashRangeX,
                patternCfg.DoubleCleave.SlashRangeY,
                patternCfg.DoubleCleave.SlashDamage,
                0.24f,
                patternCfg.DoubleCleave.SlashColor,
                dir);
        }
        else if (cmd.Skill == BossSkill.GroundStomp)
        {
            float colHeight = entity.Has<CollisionBox>() ? entity.Get<CollisionBox>().Height : b2Cfg.CollisionHeight;
            float groundY = trans.Y + colHeight / 2.0f - 10.0f;
            CombatEffectHelper.SpawnShockwave(world, trans.X, groundY);
            CombatEffectHelper.SpawnGroundShockwave(
                world,
                trans.X - 30.0f,
                groundY,
                -patternCfg.GroundStomp.ShockwaveSpeed,
                patternCfg.GroundStomp.ShockwaveRadius,
                patternCfg.GroundStomp.ShockwaveDamage,
                patternCfg.GroundStomp.FillColor,
                patternCfg.GroundStomp.OutlineColor);
            CombatEffectHelper.SpawnGroundShockwave(
                world,
                trans.X + 30.0f,
                groundY,
                patternCfg.GroundStomp.ShockwaveSpeed,
                patternCfg.GroundStomp.ShockwaveRadius,
                patternCfg.GroundStomp.ShockwaveDamage,
                patternCfg.GroundStomp.FillColor,
                patternCfg.GroundStomp.OutlineColor);
        }
        else if (cmd.Skill == BossSkill.LeapSmash)
        {
            float colHeight = entity.Has<CollisionBox>() ? entity.Get<CollisionBox>().Height : b2Cfg.CollisionHeight;
            float groundY = trans.Y + colHeight / 2.0f;
            CombatEffectHelper.SpawnShockwave(world, trans.X, groundY);

            LeapSmashConfig leapCfg = patternCfg.LeapSmash;
            for (int k = 0; k < leapCfg.ShrapnelCount; k++)
            {
                float angle = -MathF.PI * (0.15f + 0.7f * (k / (float)(leapCfg.ShrapnelCount - 1)));
                CombatEffectHelper.SpawnBossBullet(
                    world,
                    trans.X,
                    groundY - 15.0f,
                    MathF.Cos(angle) * leapCfg.ShrapnelSpeed,
                    MathF.Sin(angle) * leapCfg.ShrapnelSpeed,
                    leapCfg.ShrapnelRadius,
                    leapCfg.ShrapnelFillColor,
                    leapCfg.ShrapnelOutlineColor);
            }
        }
        else if (cmd.Skill == BossSkill.FrenzyCharge)
        {
            CombatEffectHelper.SpawnMeleeSlashHitbox(
                world,
                trans.X,
                trans.Y,
                vel.Vx,
                0.0f,
                100.0f,
                60.0f,
                b2Cfg.BodyContactDamage,
                0.25f,
                ColorPalette.RedBright,
                dir);
        }
        else if (cmd.Skill == BossSkill.DiveRam)
        {
            CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
        }
    }
}


