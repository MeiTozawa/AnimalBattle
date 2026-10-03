using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Characters;

/// <summary>
///     統一戦闘者インターフェース <see cref="ICharacter"/> の実装および契約遵守を検証するテスト。
/// </summary>
public class CombatantContractTests
{
    [Fact]
    public void Player_ImplementsICharacter_And_StateMachineWorks()
    {
        using World world = World.Create();
        Entity pEnt = world.Entity("P");
        Player player = new(pEnt, new PlayerConfig { MaxHp = 3, SpawnX = 100, SpawnY = 200 }, -1);

        // 契約検証
        ICharacter combatant = player;
        Assert.Equal((ulong)pEnt.Id, combatant.EntityId);
        Assert.Equal(Faction.Friendly, combatant.Faction);
        Assert.True(combatant.IsAlive);
        Assert.False(combatant.IsInvincible);

        // 状態マシン検証（初期接地なし）
        Assert.Equal(PlayerState.Airborne, player.CurrentState);

        player.IsGrounded = true;
        Assert.Equal(PlayerState.Grounded, player.CurrentState);

        // ダメージ契約検証
        combatant.TakeDamage(1.0f);
        Assert.Equal(2.0f, combatant.CurrentHp);

        // 無敵時間付与と状態遷移
        player.InvincibleTimer = 1.0f;
        Assert.True(combatant.IsInvincible);
        Assert.Equal(PlayerState.Hurt, player.CurrentState);

        // 無敵時の通常ダメージ無効化
        combatant.TakeDamage(1.0f, ignoreInvincible: false);
        Assert.Equal(2.0f, combatant.CurrentHp);

        // 無敵無視ダメージ適用
        combatant.TakeDamage(2.0f, ignoreInvincible: true);
        Assert.Equal(0.0f, combatant.CurrentHp);
        Assert.False(combatant.IsAlive);
        Assert.Equal(PlayerState.Dead, player.CurrentState);
    }

    [Fact]
    public void BossCharacter_ImplementsICharacter_And_DamageContractWorks()
    {
        using World world = World.Create();
        Entity bEnt = world.Entity("Boss");
        BossCharacter boss = new(bEnt, BossType.Boss1, -1);

        // 契約検証
        ICharacter combatant = boss;
        Assert.Equal((ulong)bEnt.Id, combatant.EntityId);
        Assert.Equal(Faction.Hostile, combatant.Faction);
        Assert.True(combatant.IsAlive);

        float initialHp = combatant.CurrentHp;
        Assert.True(initialHp > 0.0f);

        // ダメージ契約検証
        combatant.TakeDamage(10.0f);
        Assert.Equal(initialHp - 10.0f, combatant.CurrentHp);
    }
}
