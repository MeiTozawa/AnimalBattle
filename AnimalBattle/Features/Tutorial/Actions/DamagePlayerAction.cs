namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     チュートリアル中にプレイヤーへ被弾ダメージを与え、被弾演出を発生させるアクション。
/// </summary>
public sealed class DamagePlayerAction(float damage) : ITutorialAction {
    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        using var query = world.QueryBuilder<Health>().With<PlayerTag>().Build();
        query.Each((e, ref hp) =>
        {
            var prevHp = hp.Current;
            // チュートリアル中の即死を防止するため、最低 1.0 のHPを残す
            hp.Current = MathF.Max(1.0f, hp.Current - damage);

            EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hp.Current));
            EventBus.Publish(new PlayerDamagedEvent());
        });
    }
}
