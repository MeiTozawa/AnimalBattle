using AnimalBattle.Combat.Events;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     巨型エネルギー弾等の持続多段判定（接触跳字ダメージ）を処理する ECS システム。
///     <see cref="ContinuousDamageDealer"/> を保持する弾頭エンティティを対象とし、
///     一定ティック間隔（デフォルト 0.1s）ごとに空間ハッシュと交差する敵対 Hurtbox へ継続ダメージを適用します。
/// </summary>
public static class ContinuousDamageSystem
{
    /// <summary>
    ///     持続接触ダメージ判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddContinuousDamageSystem(this World world)
    {
        world.System<Transform, Hitbox, ContinuousDamageDealer>("ContinuousDamageSystem")
            .Without<RecycleTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteContinuousDamage);

        return world;
    }

    private static void ExecuteContinuousDamage(
        Iter it,
        Field<Transform> transforms,
        Field<Hitbox> hitboxes,
        Field<ContinuousDamageDealer> dealers)
    {
        var spatialIndex = it.World().Get<HurtboxSpatialResource>().Index;
        if (spatialIndex.ItemCount == 0) return;

        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();
        var inbox = world.Get<DamageInbox>();

        Span<int> candidateIndices = stackalloc int[16];

        for (var i = 0; i < count; i++)
        {
            ref var dealer = ref dealers[i];
            dealer.NextTickTimer -= dt;
            if (dealer.NextTickTimer > 0.0f) continue;

            dealer.NextTickTimer += dealer.TickInterval;
            var tickDamage = dealer.DamagePerSecond * dealer.TickInterval;

            ref readonly var trans = ref transforms[i];
            ref readonly var hitbox = ref hitboxes[i];
            if (!hitbox.IsActive) continue;

            var aCenterX = trans.Position.X + hitbox.Shape.Offset.X;
            var aCenterY = trans.Position.Y + hitbox.Shape.Offset.Y;
            var aHalfW = hitbox.Shape.Type == CollisionShapeType.Box
                ? hitbox.Shape.Size.X * 0.5f
                : hitbox.Shape.Radius;
            var aHalfH = hitbox.Shape.Type == CollisionShapeType.Box
                ? hitbox.Shape.Size.Y * 0.5f
                : hitbox.Shape.Radius;

            var matchCount = spatialIndex.QueryCandidates(
                aCenterX, aCenterY, aHalfW, aHalfH, hitbox.Faction, candidateIndices);

            var hasHitAny = false;
            for (var c = 0; c < matchCount; c++)
            {
                ref readonly var target = ref spatialIndex.GetItem(candidateIndices[c]);
                var vicEntity = target.Entity;
                if (!vicEntity.IsAlive()) continue;

                ref readonly var hp = ref vicEntity.Get<Health>();
                if (hp.Current <= 0.0f) continue;

                ref readonly var vicTrans = ref vicEntity.Get<Transform>();
                ref readonly var hurtbox = ref vicEntity.Get<Hurtbox>();

                if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                {
                    inbox.Enqueue(new HitEvent
                    {
                        Victim = vicEntity,
                        Damage = tickDamage,
                        SourceFaction = hitbox.Faction,
                        Knockback = Vec2f.Zero,
                        IsHeavy = false
                    });

                    EffectSpawner.SpawnHitSparkEffect(world, vicTrans.Position + hurtbox.Shape.Offset);
                    hasHitAny = true;
                }
            }

            if (hasHitAny)
            {
                EventBus.Publish(new CombatHitLandedEvent());
            }
        }
    }
}
