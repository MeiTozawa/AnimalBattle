namespace AnimalBattle.Prefabs;

/// <summary>
///     Flecs Prefab（原型）を一意に識別する厳格な型安全キー。
///     文字列によるルックアップのタイプミスを防ぎ、O(1) の高速インデックス参照を提供します。
/// </summary>
public enum PrefabKey
{
    // --- 投射物（弾幕） ---
    /// <summary>自機通常弾原型。</summary>
    PlayerBullet,

    /// <summary>Boss 通常弾幕原型。</summary>
    BossNormalBullet,

    /// <summary>Boss 品紅特殊弾原型（パルス拡縮）。</summary>
    BossMagentaBullet,

    /// <summary>Boss 黄特殊弾原型（周回衛星母弾）。</summary>
    BossYellowBullet,

    /// <summary>Boss 黄特殊弾の周回衛星弾原型。</summary>
    BossYellowSatellite,

    /// <summary>Boss 青特殊弾原型（拡散分裂）。</summary>
    BossBlueBullet,

    /// <summary>Boss 黒特殊弾原型（反撃黒弾）。</summary>
    BossBlackBullet,

    // --- 近接判定 ---
    /// <summary>自機近接斬撃ヒットボックス判定原型。</summary>
    PlayerSlashHitbox,

    // --- 特効演出 (VFX) ---
    /// <summary>点粒子スパーク演出原型。</summary>
    SparkVfx,

    /// <summary>リング拡散演出原型。</summary>
    RingVfx,

    /// <summary>判定付与エリア演出原型。</summary>
    AreaVfx,

    // --- プレイヤーキャラクター ---
    /// <summary>Cat プレイヤーキャラクター原型。</summary>
    CatPlayer,

    /// <summary>Bunny プレイヤーキャラクター原型。</summary>
    BunnyPlayer,

    // --- Boss キャラクター ---
    /// <summary>Boss 1 (Ancient Cat Overlord) 原型。</summary>
    Boss1,

    /// <summary>Boss 2 (Ground Crusher Golem) 原型。</summary>
    Boss2
}
