using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     遠隔射撃を得意とする猫型プレイヤー自機（Cat）の具象受控躯体クラス。
///     <see cref="CatWeaponState"/> を直接保持し、高頻度パスでの分岐・型判定なしに射撃クールダウン管理を提供します。
/// </summary>
public sealed class Cat : PlayerPawn
{
    /// <summary>
    ///     Cat プレイヤー自機を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライト画像ハンドル。</param>
    public Cat(Entity entity, PlayerConfig pCfg, int spriteHandle) : base(entity, pCfg, spriteHandle, CharacterType.Cat)
    {
        Entity.Set(CatWeaponState.FromConfig(pCfg));
    }

    /// <summary>射撃クールダウン残り秒数の直接取得および設定（entity.Has なしの直接アクセス）。</summary>
    public override float AttackCooldown
    {
        get => Entity.Get<CatWeaponState>().CooldownRemaining;
        set => Entity.GetMut<CatWeaponState>().CooldownRemaining = Math.Max(0.0f, value);
    }

    /// <summary>
    ///     Cat 自機を初期位置へリセットし、武器状態を再同期します。
    /// </summary>
    /// <param name="pCfg">リセット用設定。</param>
    public override void Reset(PlayerConfig pCfg)
    {
        base.Reset(pCfg);
        Entity.Set(CatWeaponState.FromConfig(pCfg));
    }
}
