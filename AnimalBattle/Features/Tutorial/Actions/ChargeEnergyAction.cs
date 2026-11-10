namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     チュートリアル中にプレイヤーのエネルギーゲージを所定スロット分チャージするアクション。
/// </summary>
public sealed class ChargeEnergyAction(int segments) : ITutorialAction
{
    private readonly int _segments = segments;

    /// <inheritdoc />
    public void Execute(World world, Entity root)
    {
        using var query = world.QueryBuilder<EnergyGauge>().With<PlayerTag>().Without<DeadTag>().Build();
        query.Each((ref EnergyGauge gauge) =>
        {
            var oldReady = gauge.ReadySegments;
            var targetEnergy = gauge.SegmentCapacity * _segments;
            gauge.CurrentEnergy = MathF.Max(gauge.CurrentEnergy, targetEnergy);
            var newReady = gauge.ReadySegments;
            if (newReady > oldReady)
            {
                EventBus.Publish(new EnergySegmentChargedEvent(newReady));
            }
        });
    }
}
