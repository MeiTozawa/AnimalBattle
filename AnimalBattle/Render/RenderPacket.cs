using System.Runtime.InteropServices;

namespace AnimalBattle.Render;

/// <summary>
///     レンダリングコマンド種別を表す列挙型。
/// </summary>
public enum RenderPacketType : ushort {
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
    SetDrawBright = 9
}

/// <summary>
///     矩形描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawBoxPacket {
    /// <summary>矩形左上 X 座標</summary>
    public int X1;

    /// <summary>矩形左上 Y 座標</summary>
    public int Y1;

    /// <summary>矩形右下 X 座標</summary>
    public int X2;

    /// <summary>矩形右下 Y 座標</summary>
    public int Y2;

    /// <summary>矩形描画カラー (0xAARRGGBB / 0x00RRGGBB)</summary>
    public uint Color;

    /// <summary>塗りつぶしフラグ (1: 塗りつぶし, 0: 枠線)</summary>
    public byte Fill;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     円描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawCirclePacket {
    /// <summary>円中心 X 座標</summary>
    public int CenterX;

    /// <summary>円中心 Y 座標</summary>
    public int CenterY;

    /// <summary>円半径</summary>
    public int Radius;

    /// <summary>円描画カラー</summary>
    public uint Color;

    /// <summary>円塗りつぶしフラグ (1: 塗りつぶし, 0: 枠線)</summary>
    public byte Fill;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     線分描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawLinePacket {
    /// <summary>線分始点 X 座標</summary>
    public int X1;

    /// <summary>線分始点 Y 座標</summary>
    public int Y1;

    /// <summary>線分終点 X 座標</summary>
    public int X2;

    /// <summary>線分終点 Y 座標</summary>
    public int Y2;

    /// <summary>線分描画カラー</summary>
    public uint Color;

    /// <summary>線の太さ</summary>
    public int Thickness;
}

/// <summary>
///     単純画像描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawGraphPacket {
    /// <summary>画像描画先 X 座標</summary>
    public int X;

    /// <summary>画像描画先 Y 座標</summary>
    public int Y;

    /// <summary>画像ハンドル</summary>
    public int Handle;

    /// <summary>透過フラグ</summary>
    public byte Trans;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     回転拡縮画像描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawRotaGraphPacket {
    /// <summary>回転描画中心 X 座標</summary>
    public int CenterX;

    /// <summary>回転描画中心 Y 座標</summary>
    public int CenterY;

    /// <summary>拡大率</summary>
    public float Scale;

    /// <summary>回転角度 (ラジアン)</summary>
    public float Angle;

    /// <summary>画像ハンドル</summary>
    public int Handle;

    /// <summary>透過フラグ</summary>
    public byte Trans;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     矩形切り出し回転拡縮画像描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawRectRotaGraphPacket {
    /// <summary>矩形切り出し回転描画中心 X 座標</summary>
    public int CenterX;

    /// <summary>矩形切り出し回転描画中心 Y 座標</summary>
    public int CenterY;

    /// <summary>切り出し開始 X 座標</summary>
    public int SrcX;

    /// <summary>切り出し開始 Y 座標</summary>
    public int SrcY;

    /// <summary>切り出し幅</summary>
    public int SrcW;

    /// <summary>切り出し高さ</summary>
    public int SrcH;

    /// <summary>拡大率</summary>
    public float Scale;

    /// <summary>回転角度 (ラジアン)</summary>
    public float Angle;

    /// <summary>画像ハンドル</summary>
    public int Handle;

    /// <summary>透過フラグ</summary>
    public byte Trans;

    /// <summary>左右反転フラグ</summary>
    public byte TurnX;

    private byte _padding0;
    private byte _padding1;
}

/// <summary>
///     文字列描画コマンドデータ（文字列実体は一括テキストバッファを参照）。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawStringPacket {
    /// <summary>文字列描画 X 座標</summary>
    public int X;

    /// <summary>文字列描画 Y 座標</summary>
    public int Y;

    /// <summary>文字列描画カラー</summary>
    public uint Color;

    /// <summary>一括テキストバッファ内の文字オフセット</summary>
    public int TextOffset;

    /// <summary>文字列の長さ (文字数)</summary>
    public int TextLength;
}

/// <summary>
///     描画ブレンドモード設定コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SetBlendModePacket {
    /// <summary>ブレンドモード値</summary>
    public int Mode;

    /// <summary>ブレンドパラメータ (アルファ値等: 0～255)</summary>
    public int Pal;
}

/// <summary>
///     描画輝度設定コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SetBrightPacket {
    /// <summary>赤輝度 (0～255)</summary>
    public int R;

    /// <summary>緑輝度 (0～255)</summary>
    public int G;

    /// <summary>青輝度 (0～255)</summary>
    public int B;
}

/// <summary>
///     C# - C++ 間で一括転送される 48 バイト固定長の Blittable 描画コマンドパケット。
///     各種描画コマンドデータは共用体（Explicit Layout Union）として同一オフセットに配置されます。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct RenderPacket {
    /// <summary>コマンド種別</summary>
    [FieldOffset(0)] public RenderPacketType Type;

    /// <summary>予約領域（アライメント用）</summary>
    [FieldOffset(2)] public ushort Reserved;

    /// <summary>矩形描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawBoxPacket Box;

    /// <summary>円描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawCirclePacket Circle;

    /// <summary>線分描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawLinePacket Line;

    /// <summary>単純画像描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawGraphPacket Graph;

    /// <summary>回転拡縮画像描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawRotaGraphPacket RotaGraph;

    /// <summary>矩形切り出し回転拡縮画像描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawRectRotaGraphPacket RectRotaGraph;

    /// <summary>文字列描画コマンドデータ</summary>
    [FieldOffset(4)] public DrawStringPacket StringData;

    /// <summary>描画ブレンドモード設定コマンドデータ</summary>
    [FieldOffset(4)] public SetBlendModePacket Blend;

    /// <summary>描画輝度設定コマンドデータ</summary>
    [FieldOffset(4)] public SetBrightPacket Bright;
}