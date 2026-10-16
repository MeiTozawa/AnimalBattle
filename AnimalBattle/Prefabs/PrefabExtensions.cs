using AnimalBattle.Ai;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Prefabs;

/// <summary>
///     ゲーム内ドメイン列挙型と Flecs PrefabKey 間の安全なマッピング拡張メソッド。
///     switch 式と AssertHelper.FailWithDefault パターンにより、未知の列挙型値に対する防御的フォールバックを提供します。
/// </summary>
public static class PrefabExtensions
{
    /// <summary>
    ///     BossType 列挙型を対応する Flecs PrefabKey に変換します。
    ///     未登録の種別が渡された場合はアサート失敗を出力し、安全なデフォルト値（Boss1）を返します。
    /// </summary>
    /// <param name="bossType">対象の Boss 種別。</param>
    /// <returns>対応する PrefabKey。</returns>
    public static PrefabKey ToPrefabKey(this BossType bossType) => bossType switch {
        BossType.Boss1 => PrefabKey.Boss1,
        BossType.Boss2 => PrefabKey.Boss2,
        _ => AssertHelper.FailWithDefault($"[PrefabExtensions 警告] 未知の BossType: {bossType}", PrefabKey.Boss1)
    };

    /// <summary>
    ///     CharacterType 列挙型を対応する Flecs PrefabKey に変換します。
    ///     未登録の種別が渡された場合はアサート失敗を出力し、安全なデフォルト値（CatPlayer）を返します。
    /// </summary>
    /// <param name="charType">対象のプレイヤーキャラクター種別。</param>
    /// <returns>対応する PrefabKey。</returns>
    public static PrefabKey ToPrefabKey(this CharacterType charType) => charType switch {
        CharacterType.Cat => PrefabKey.CatPlayer,
        CharacterType.Bunny => PrefabKey.BunnyPlayer,
        _ => AssertHelper.FailWithDefault($"[PrefabExtensions 警告] 未知の CharacterType: {charType}", PrefabKey.CatPlayer)
    };
}
