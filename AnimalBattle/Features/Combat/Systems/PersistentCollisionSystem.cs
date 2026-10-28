namespace AnimalBattle.Combat.Systems;

/// <summary>
/// 多フレーム持続判定・近接攻撃専用の衝突判定 ECS システム。
/// HitHistoryを保持する近接斬撃や貫通攻撃判定を対象とし、
/// 同一エンティティに対する毎フレームの重複ダメージを防止しつつ、
/// <see cref="Hurtbox" /> との幾何学的交差判定およびダメージ適用を行います。
/// </summary>
public static class PersistentCollisionSystem
{
    /// <summary>
    /// 持続・近接衝突判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddPersistentCollisionSystem(this World world)
    {
        world.System<Transform, Hitbox, HitHistory>("PersistentCollisionSystem")
            .Without<RecycleTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecutePersistentHit);

        return world;
    }

    /// <summary>
    /// 持続および近接攻撃判定パイプラインです。
    /// 空間ハッシュから候補Hurtboxを抽出して幾何交差および重複ヒット防止判定を行います。
    /// </summary>
    public static void ExecutePersistentHit(
        Iter it,
        Field<Transform> atkTrans,
        Field<Hitbox> hitboxes,
        Field<HitHistory> histories)
    {
        var spatialIndex = it.World().Get<HurtboxSpatialResource>().Index;
        if (spatialIndex.ItemCount == 0) return;

        var count = it.Count();
        var world = it.World();

        Span<int> candidateIndices = stackalloc int[16];

        // 被弾コンポーネント追加や構造変更によるイテレーション破壊を防止するため、遅延実行モードを開始する
        using (new DeferScope(world))
        {
            for (var i = 0; i < count; i++)
            {
                ref var hitbox = ref hitboxes[i];
                if (!hitbox.IsActive) continue;

                // 単発弾幕が混入した場合は誤判定防止のためスキップします
                if (hitbox.DestroyOnHit)
                {
                    Debug.Fail($"[PersistentCollisionSystem 警告] DestroyOnHit == true の単発弾丸に HitHistory が付与されています: {it.Entity(i)}");
                    continue;
                }

                ref var trans = ref atkTrans[i];
                ref var history = ref histories[i];
                var shouldDestroyAttacker = false;

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

                for (var c = 0; c < matchCount; c++)
                {
                    ref readonly var target = ref spatialIndex.GetItem(candidateIndices[c]);
                    var vicEntity = target.Entity;
                    if (!vicEntity.IsAlive()) continue;

                    // 持続・近接判定専用：同一対象への多段重複ヒットを防止
                    if (history.HasHit(vicEntity.Id)) continue;

                    ref readonly var hp = ref vicEntity.Get<Health>();
                    if (hp.Current <= 0.0f) continue;

                    ref readonly var vicTrans = ref vicEntity.Get<Transform>();
                    ref readonly var hurtbox = ref vicEntity.Get<Hurtbox>();

                    if (PhysicsService.Intersects(trans, hitbox.Shape, vicTrans, hurtbox.Shape))
                    {
                        history.TryRecordHit(vicEntity.Id);
                        CombatCollisionSystem.ApplyHit(world, vicEntity, in vicTrans, in trans, in hurtbox, in hitbox,
                            ref shouldDestroyAttacker);
                    }
                }
            }
        }
    }
}
