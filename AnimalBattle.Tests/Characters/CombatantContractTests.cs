using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     <see cref="Health"/> コンポーネントおよび受控 Pawn（Player / BossCharacter）の体力・受撃契約遵守を検証するテスト。
/// </summary>
public class CombatantContractTests
{
    private sealed class DummyDamageable(float maxHp, float multiplier = 1.0f, bool isInvulnerable = false) : IDamageable {
        public Health Health { get; } = new(maxHp);
        public float DamageMultiplier { get; set; } = multiplier;
        public bool IsInvulnerable { get; set; } = isInvulnerable;
    }

    [Fact]
    public void IDamageable_TakeDamage_ReducesHp_RespectingInvincibleAndVulnerable()
    {
        var dummy = new DummyDamageable(100.0f);
        IDamageable target = dummy;
        Assert.Equal(100.0f, target.Health.Current);

        // 1. 通常ダメージ適用
        var applied = target.TakeDamage(30.0f);
        Assert.True(applied);
        Assert.Equal(70.0f, target.Health.Current);

        // 2. Health.IsInvincible による無敵保護
        target.Health.InvincibleTimer = 1.0f;
        applied = target.TakeDamage(20.0f);
        Assert.False(applied);
        Assert.Equal(70.0f, target.Health.Current);

        // 無敵時間解除
        target.Health.InvincibleTimer = 0.0f;

        // 3. IsInvulnerable による無敵保護
        dummy.IsInvulnerable = true;
        applied = target.TakeDamage(20.0f);
        Assert.False(applied);
        Assert.Equal(70.0f, target.Health.Current);

        // IsInvulnerable 解除
        dummy.IsInvulnerable = false;

        // 4. DamageMultiplier による易傷（2倍ダメージ）適用
        dummy.DamageMultiplier = 2.0f;
        applied = target.TakeDamage(20.0f);
        Assert.True(applied);
        Assert.Equal(30.0f, target.Health.Current); // 70 - (20 * 2) = 30
    }

    [Fact]
    public void Player_ImplementsIDamageable_And_StateMachineWorks()
    {
        using World world = World.Create();
        Entity pEnt = world.Entity("P");
        Player player = new(pEnt, new PlayerConfig { MaxHp = 3, SpawnX = 100, SpawnY = 200 }, -1);

        // 契約検証 (IDamageable)
        IDamageable damageable = player;
        Assert.Equal((ulong)pEnt.Id, player.EntityId);
        Assert.Equal(Faction.Friendly, player.Faction);
        Assert.True(damageable.Health.IsAlive);
        Assert.False(damageable.IsInvulnerable);
        Assert.Equal(3.0f, damageable.Health.Current);

        // 状態マシン検証（初期接地なし）
        Assert.Equal(PlayerState.Airborne, player.CurrentState);

        player.IsGrounded = true;
        Assert.Equal(PlayerState.Grounded, player.CurrentState);

        // ダメージ契約検証
        damageable.TakeDamage(1.0f);
        Assert.Equal(2.0f, player.Health.Current);

        // 無敵時間付与と状態遷移
        Assert.True(player.Health.IsInvincible);
        Assert.Equal(PlayerState.Hurt, player.CurrentState);

        // 無敵時のダメージ無効化
        damageable.TakeDamage(1.0f);
        Assert.Equal(2.0f, player.Health.Current);

        // 無敵時間終了後の致命傷ダメージ適用
        player.Health.InvincibleTimer = 0.0f;
        damageable.TakeDamage(2.0f);
        Assert.Equal(0.0f, player.Health.Current);
        Assert.False(player.Health.IsAlive);
        Assert.Equal(PlayerState.Dead, player.CurrentState);
    }

    [Fact]
    public void BossCharacter_ImplementsIDamageable_And_DamageContractWorks()
    {
        using World world = World.Create();
        Entity bEnt = world.Entity("Boss");
        BossCharacter boss = new(bEnt, BossType.Boss1, -1);

        // 契約検証 (IDamageable)
        IDamageable damageable = boss;
        Assert.Equal((ulong)bEnt.Id, boss.EntityId);
        Assert.Equal(Faction.Hostile, boss.Faction);
        Assert.True(damageable.Health.IsAlive);

        float initialHp = boss.Health.Current;
        Assert.True(initialHp > 0.0f);

        // ダメージ契約検証
        damageable.TakeDamage(10.0f);
        Assert.Equal(initialHp - 10.0f, boss.Health.Current);
    }
}
