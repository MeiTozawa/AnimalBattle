namespace AnimalBattle.Components;

/// <summary>
///     弾幕の特殊挙動・視覚表現の種別を定義する列挙型。
/// </summary>
public enum BulletType : byte {
    /// <summary>標準の直進単一円形弾。</summary>
    Normal = 0,

    /// <summary>品紅色弾幕：呼吸するように周期的に拡大・縮小（拡縮判定同期）。</summary>
    Magenta = 1,

    /// <summary>黄色弾幕：主円と衛星小円の二重円構成で、自転・公転回転する。</summary>
    Yellow = 2,

    /// <summary>青色弾幕：進行方向と直交する法線方向へ微小な正弦波（左右）往復運動を行う。</summary>
    Cyan = 3,

    /// <summary>黑色弾幕：呼吸拡縮・二重円回転・左右揺動の 3 特効を全統合した混沌弾。</summary>
    Black = 4
}