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
    [Fact]
    public void DamageSystem_ApplyDamage_ReducesHp_RespectingInvincible()
    {
        using var world = World.Create();
        var entity = world.Entity("Victim")
            .Set(new Health(100.0f));

        Assert.Equal(100.0f, entity.Get<Health>().Current);

        var applied = DamageSystem.ApplyDamage(entity, 30.0f);
        Assert.True(applied);
        Assert.Equal(70.0f, entity.Get<Health>().Current);

        entity.GetMut<Health>().InvincibleTimer = 1.0f;
        applied = DamageSystem.ApplyDamage(entity, 20.0f, ignoreInvincible: false);
        Assert.False(applied);
        Assert.Equal(70.0f, entity.Get<Health>().Current);

        applied = DamageSystem.ApplyDamage(entity, 20.0f, ignoreInvincible: true);
        Assert.True(applied);
        Assert.Equal(50.0f, entity.Get<Health>().Current);
    }

    [Fact]
    public void Player_FacadeReflectsHealth_And_StateMachineWorks()
    {
        using World world = World.Create();
        Entity pEnt = world.Entity("P");
        Player player = new(pEnt, new PlayerConfig { MaxHp = 3, SpawnX = 100, SpawnY = 200 }, -1);

        // 契約検証
        Assert.Equal((ulong)pEnt.Id, player.EntityId);
        Assert.Equal(Faction.Friendly, player.Faction);
        Assert.True(player.IsAlive);
        Assert.False(player.IsInvincible);
        Assert.Equal(3.0f, player.Health.Current);

        // 状態マシン検証（初期接地なし）
        Assert.Equal(PlayerState.Airborne, player.CurrentState);

        player.IsGrounded = true;
        Assert.Equal(PlayerState.Grounded, player.CurrentState);

        // ダメージ契約検証
        player.TakeDamage(1.0f);
        Assert.Equal(2.0f, player.CurrentHp);

        // 無敵時間付与と状態遷移
        player.InvincibleTimer = 1.0f;
        Assert.True(player.IsInvincible);
        Assert.Equal(PlayerState.Hurt, player.CurrentState);

        // 無敵時の通常ダメージ無効化
        player.TakeDamage(1.0f, ignoreInvincible: false);
        Assert.Equal(2.0f, player.CurrentHp);

        // 無敵無視ダメージ適用
        player.TakeDamage(2.0f, ignoreInvincible: true);
        Assert.Equal(0.0f, player.CurrentHp);
        Assert.False(player.IsAlive);
        Assert.Equal(PlayerState.Dead, player.CurrentState);
    }

    [Fact]
    public void BossCharacter_FacadeReflectsHealth_And_DamageContractWorks()
    {
        using World world = World.Create();
        Entity bEnt = world.Entity("Boss");
        BossCharacter boss = new(bEnt, BossType.Boss1, -1);

        // 契約検証
        Assert.Equal((ulong)bEnt.Id, boss.EntityId);
        Assert.Equal(Faction.Hostile, boss.Faction);
        Assert.True(boss.IsAlive);

        float initialHp = boss.CurrentHp;
        Assert.True(initialHp > 0.0f);

        // ダメージ契約検証
        boss.TakeDamage(10.0f);
        Assert.Equal(initialHp - 10.0f, boss.CurrentHp);
    }
}
