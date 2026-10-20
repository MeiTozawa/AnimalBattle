namespace AnimalBattle.Combat;

/// <summary>
///     斬撃エフェクトの展開方向（朝下、朝上、朝右、朝左）。
///     スプライトシート行（Row 0: Down, Row 1: Up, Row 2: Right, Row 3: Left）に対応します。
/// </summary>
public enum SlashDirection {
    /// <summary>朝下劈砍 (Row 0)</summary>
    Down = 0,

    /// <summary>朝上挑斬 (Row 1)</summary>
    Up = 1,

    /// <summary>朝右横斬 (Row 2)</summary>
    Right = 2,

    /// <summary>朝左横斬 (Row 3)</summary>
    Left = 3
}
