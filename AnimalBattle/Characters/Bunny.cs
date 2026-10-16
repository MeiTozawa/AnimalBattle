using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Prefabs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     近接打撃を得意とする兎型プレイヤー自機（Bunny）の具象受控躯体クラス。
///     常駐近接攻撃判定エンティティおよび <see cref="BunnyWeaponState"/> を直接保持・制御します。
/// </summary>
public sealed class Bunny : PlayerPawn
{
    /// <summary>常駐する自機近接斬撃判定エンティティ句柄を取得します。</summary>
    public Entity MeleeAttackEntity { get; }

    /// <summary>
    ///     Bunny プレイヤー自機を初期化し、常駐近接判定を事前生成して無効化状態で待機させます。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライト画像ハンドル。</param>
    public Bunny(Entity entity, PlayerConfig pCfg, int spriteHandle) : base(entity, pCfg, spriteHandle, CharacterType.Bunny)
    {
        var melee = PrefabRegistry.Instantiate(entity.CsWorld(), PrefabKey.PlayerSlashHitbox);
        melee.Disable();
        MeleeAttackEntity = melee;

        Entity.Set(BunnyWeaponState.FromConfig(pCfg))
           .Set(new PlayerMeleeWeapon(melee));
    }

    /// <summary>近接攻撃クールダウン残り秒数の直接取得および設定（entity.Has なしの直接アクセス）。</summary>
    public override float AttackCooldown
    {
        get => Entity.Get<BunnyWeaponState>().CooldownRemaining;
        set => Entity.GetMut<BunnyWeaponState>().CooldownRemaining = Math.Max(0.0f, value);
    }

    /// <summary>
    ///     Bunny 自機を初期位置へリセットし、武器状態および常駐近接判定を安全に無効化します。
    /// </summary>
    /// <param name="pCfg">リセット用設定。</param>
    public override void Reset(PlayerConfig pCfg)
    {
        base.Reset(pCfg);
        Entity.Set(BunnyWeaponState.FromConfig(pCfg));
        if (MeleeAttackEntity.IsAlive())
        {
            MeleeAttackEntity.Disable();
        }
    }
}
