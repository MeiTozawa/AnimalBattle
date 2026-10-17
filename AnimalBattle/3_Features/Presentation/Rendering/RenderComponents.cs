namespace AnimalBattle.Components;

/// <summary>
///     スプライトおよび幾何学描画の Z 深度優先順位を定義するコンポーネント。
/// </summary>
public struct RenderOrder(int value) {
    public int Value = value;

    public const int Background = -100;
    public const int TileMap = 0;
    public const int BossTelegraph = 10;
    public const int Character = 20;
    public const int Bullets = 30;
    public const int Vfx = 40;
    public const int MeleeSlash = 50;
    public const int Ui = 100;
    public const int Reticle = 200;
}

/// <summary>
///     照準器（エイムカーソル）エンティティを識別するタグコンポーネント。
/// </summary>
public readonly struct AimReticleTag;

/// <summary>
///     照準器（AimReticle）のスプリング拡縮振幅および速度を保持する純粋コンポーネント。
/// </summary>
public struct AimReticleState
{
    public float Expansion;
    public float ExpansionVelocity;
}
