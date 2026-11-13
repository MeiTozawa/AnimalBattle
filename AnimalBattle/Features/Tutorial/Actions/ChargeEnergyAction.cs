using AnimalBattle.Features.Tutorial.Systems;

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
        var player = root.IsAlive() ? root.Lookup("TutorialPlayer") : default;
        if (player.IsAlive() && player.Has<EnergyGauge>() && !player.Has<DeadTag>())
        {
            ref var gauge = ref player.GetMut<EnergyGauge>();
            ChargeGauge(ref gauge);
            return;
        }

        var query = world.Get<TutorialSystemContext>().PlayerEnergyQuery;
        query.Each((ref EnergyGauge gauge) => ChargeGauge(ref gauge));
    }

    private void ChargeGauge(ref EnergyGauge gauge)
    {
        var oldReady = gauge.ReadySegments;
        var targetEnergy = gauge.SegmentCapacity * _segments;
        gauge.CurrentEnergy = MathF.Max(gauge.CurrentEnergy, targetEnergy);
        var newReady = gauge.ReadySegments;
        if (newReady > oldReady)
        {
            EventBus.Publish(new EnergySegmentChargedEvent(newReady));
        }
    }
}
