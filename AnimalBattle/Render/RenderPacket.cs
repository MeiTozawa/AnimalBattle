using System.Runtime.InteropServices;

namespace AnimalBattle.Render;

/// <summary>
/// レンダリングコマンド種別を表す列挙型。
/// </summary>
public enum RenderPacketType : ushort
{
    /// <summary>無効なコマンド</summary>
    None = 0,

    /// <summary>矩形描画</summary>
    DrawBox = 1,

    /// <summary>円描画</summary>
    DrawCircle = 2,

    /// <summary>線分描画</summary>
    DrawLine = 3,

    /// <summary>単純画像描画</summary>
    DrawGraph = 4,

    /// <summary>回転拡縮画像描画</summary>
    DrawRotaGraph = 5,

    /// <summary>矩形切り出し回転拡縮画像描画</summary>
    DrawRectRotaGraph = 6,

    /// <summary>文字列描画</summary>
    DrawString = 7,

    /// <summary>描画ブレンドモード設定</summary>
    SetDrawBlendMode = 8,

    /// <summary>描画輝度設定</summary>
    SetDrawBright = 9,
}

/// <summary>
/// C# - C++ 間で一括転送される 48 バイト固定長の Blittable 描画コマンドパケット。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct RenderPacket
{
    /// <summary>コマンド種別</summary>
    [FieldOffset(0)]
    public RenderPacketType Type;

    /// <summary>予約領域（アライメント用）</summary>
    [FieldOffset(2)]
    public ushort Reserved;

    #region DrawBox

    /// <summary>矩形左上 X 座標</summary>
    [FieldOffset(4)]
    public int BoxX1;

    /// <summary>矩形左上 Y 座標</summary>
    [FieldOffset(8)]
    public int BoxY1;

    /// <summary>矩形右下 X 座標</summary>
    [FieldOffset(12)]
    public int BoxX2;

    /// <summary>矩形右下 Y 座標</summary>
    [FieldOffset(16)]
    public int BoxY2;

    /// <summary>矩形描画カラー (0xAARRGGBB / 0x00RRGGBB)</summary>
    [FieldOffset(20)]
    public uint BoxColor;

    /// <summary>塗りつぶしフラグ (1: 塗りつぶし, 0: 枠線)</summary>
    [FieldOffset(24)]
    public byte BoxFill;

    #endregion

    #region DrawCircle

    /// <summary>円中心 X 座標</summary>
    [FieldOffset(4)]
    public int CircleCenterX;

    /// <summary>円中心 Y 座標</summary>
    [FieldOffset(8)]
    public int CircleCenterY;

    /// <summary>円半径</summary>
    [FieldOffset(12)]
    public int CircleRadius;

    /// <summary>円描画カラー</summary>
    [FieldOffset(16)]
    public uint CircleColor;

    /// <summary>円塗りつぶしフラグ (1: 塗りつぶし, 0: 枠線)</summary>
    [FieldOffset(20)]
    public byte CircleFill;

    #endregion

    #region DrawLine

    /// <summary>線分始点 X 座標</summary>
    [FieldOffset(4)]
    public int LineX1;

    /// <summary>線分始点 Y 座標</summary>
    [FieldOffset(8)]
    public int LineY1;

    /// <summary>線分終点 X 座標</summary>
    [FieldOffset(12)]
    public int LineX2;

    /// <summary>線分終点 Y 座標</summary>
    [FieldOffset(16)]
    public int LineY2;

    /// <summary>線分描画カラー</summary>
    [FieldOffset(20)]
    public uint LineColor;

    /// <summary>線分の太さ</summary>
    [FieldOffset(24)]
    public int LineThickness;

    #endregion

    #region DrawGraph

    /// <summary>画像描画先 X 座標</summary>
    [FieldOffset(4)]
    public int GraphX;

    /// <summary>画像描画先 Y 座標</summary>
    [FieldOffset(8)]
    public int GraphY;

    /// <summary>画像ハンドル</summary>
    [FieldOffset(12)]
    public int GraphHandle;

    /// <summary>透過フラグ</summary>
    [FieldOffset(16)]
    public byte GraphTrans;

    #endregion

    #region DrawRotaGraph

    /// <summary>回転描画中心 X 座標</summary>
    [FieldOffset(4)]
    public int RotaCenterX;

    /// <summary>回転描画中心 Y 座標</summary>
    [FieldOffset(8)]
    public int RotaCenterY;

    /// <summary>拡大率</summary>
    [FieldOffset(12)]
    public float RotaScale;

    /// <summary>回転角度 (ラジアン)</summary>
    [FieldOffset(16)]
    public float RotaAngle;

    /// <summary>画像ハンドル</summary>
    [FieldOffset(20)]
    public int RotaHandle;

    /// <summary>透過フラグ</summary>
    [FieldOffset(24)]
    public byte RotaTrans;

    #endregion

    #region DrawRectRotaGraph

    /// <summary>矩形切り出し回転描画中心 X 座標</summary>
    [FieldOffset(4)]
    public int RectRotaCenterX;

    /// <summary>矩形切り出し回転描画中心 Y 座標</summary>
    [FieldOffset(8)]
    public int RectRotaCenterY;

    /// <summary>切り出し開始 X 座標</summary>
    [FieldOffset(12)]
    public int RectRotaSrcX;

    /// <summary>切り出し開始 Y 座標</summary>
    [FieldOffset(16)]
    public int RectRotaSrcY;

    /// <summary>切り出し幅</summary>
    [FieldOffset(20)]
    public int RectRotaSrcW;

    /// <summary>切り出し高さ</summary>
    [FieldOffset(24)]
    public int RectRotaSrcH;

    /// <summary>拡大率</summary>
    [FieldOffset(28)]
    public float RectRotaScale;

    /// <summary>回転角度 (ラジアン)</summary>
    [FieldOffset(32)]
    public float RectRotaAngle;

    /// <summary>画像ハンドル</summary>
    [FieldOffset(36)]
    public int RectRotaHandle;

    /// <summary>透過フラグ</summary>
    [FieldOffset(40)]
    public byte RectRotaTrans;

    /// <summary>左右反転フラグ</summary>
    [FieldOffset(41)]
    public byte RectRotaTurnX;

    #endregion

    #region DrawString

    /// <summary>文字列描画 X 座標</summary>
    [FieldOffset(4)]
    public int StringX;

    /// <summary>文字列描画 Y 座標</summary>
    [FieldOffset(8)]
    public int StringY;

    /// <summary>文字列描画カラー</summary>
    [FieldOffset(12)]
    public uint StringColor;

    /// <summary>一括テキストバッファ内の文字オフセット</summary>
    [FieldOffset(16)]
    public int StringOffset;

    /// <summary>文字列の長さ (文字数)</summary>
    [FieldOffset(20)]
    public int StringLength;

    #endregion

    #region SetDrawBlendMode

    /// <summary>ブレンドモード値</summary>
    [FieldOffset(4)]
    public int BlendMode;

    /// <summary>ブレンドパラメータ (アルファ値等: 0～255)</summary>
    [FieldOffset(8)]
    public int BlendPal;

    #endregion

    #region SetDrawBright

    /// <summary>赤輝度 (0～255)</summary>
    [FieldOffset(4)]
    public int BrightR;

    /// <summary>緑輝度 (0～255)</summary>
    [FieldOffset(8)]
    public int BrightG;

    /// <summary>青輝度 (0～255)</summary>
    [FieldOffset(12)]
    public int BrightB;

    #endregion
}
