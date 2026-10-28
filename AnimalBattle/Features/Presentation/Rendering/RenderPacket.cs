using System.Runtime.InteropServices;

namespace AnimalBattle.Presentation.Rendering;

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
    SetDrawBright = 9,

    /// <summary>連続するCompactCirclePacket領域を対象とする円描画バッチコマンド</summary>
    DrawCircleBatch = 10
}

/// <summary>
///     矩形描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawBoxPacket {
    /// <summary>矩形左上座標</summary>
    public Vec2i P1;

    /// <summary>矩形右下座標</summary>
    public Vec2i P2;

    /// <summary>32ビットARGB形式の矩形描画カラー</summary>
    public uint Color;

    /// <summary>塗りつぶし時は1、枠線のみ描画時はゼロを指定するフラグ</summary>
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
    /// <summary>円中心座標</summary>
    public Vec2i Center;

    /// <summary>円半径</summary>
    public int Radius;

    /// <summary>円描画カラー</summary>
    public uint Color;

    /// <summary>塗りつぶし時は1、枠線のみ描画時はゼロを指定するフラグ</summary>
    public byte Fill;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     弾幕やパーティクルの大量描画においてL1キャッシュ効率を最大化する20バイト配置の円描画パケットです。
///     48 バイトの汎用 RenderPacket を介さず、連続メモリ上で高密度ストリーミング転送されます。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct CompactCirclePacket {
    /// <summary>円中心座標</summary>
    public Vec2i Center;

    /// <summary>円半径</summary>
    public int Radius;

    /// <summary>円描画カラー</summary>
    public uint Color;

    /// <summary>塗りつぶし時は1、枠線のみ描画時はゼロを指定するフラグ</summary>
    public byte Fill;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;

    public CompactCirclePacket(Vec2i center, int radius, uint color, bool fill)
    {
        Center = center;
        Radius = radius;
        Color = color;
        Fill = (byte)(fill ? 1 : 0);
        _padding0 = 0;
        _padding1 = 0;
        _padding2 = 0;
    }
}

/// <summary>
///     線分描画コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawLinePacket {
    /// <summary>線分始点座標</summary>
    public Vec2i P1;

    /// <summary>線分終点座標</summary>
    public Vec2i P2;

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
    /// <summary>画像描画先座標</summary>
    public Vec2i Pos;

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
    /// <summary>回転描画中心座標</summary>
    public Vec2i Center;

    /// <summary>拡大率</summary>
    public float Scale;

    /// <summary>ラジアン単位の回転角度</summary>
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
    /// <summary>矩形切り出し回転描画中心座標</summary>
    public Vec2i Center;

    /// <summary>切り出し開始座標</summary>
    public Vec2i SrcPos;

    /// <summary>切り出し領域の幅および高さ</summary>
    public Vec2i SrcSize;

    /// <summary>水平および垂直方向の独立した拡縮率</summary>
    public Vec2f Scale;

    /// <summary>ラジアン単位の回転角度</summary>
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
///     文字列描画のアライメント基準。
/// </summary>
public enum TextAlignment : byte {
    /// <summary>左上基準</summary>
    TopLeft = 0,

    /// <summary>左揃えおよび垂直中央揃え</summary>
    Left = 1,

    /// <summary>水平中央および垂直中央揃え</summary>
    Center = 2,

    /// <summary>右揃えおよび垂直中央揃え</summary>
    Right = 3
}

/// <summary>
///     文字列描画コマンドデータであり、文字列の実体は一括テキストバッファを参照します。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawStringPacket {
    /// <summary>文字列描画座標</summary>
    public Vec2i Pos;

    /// <summary>文字列描画カラー</summary>
    public uint Color;

    /// <summary>一括テキストバッファ内の文字オフセット</summary>
    public int TextOffset;

    /// <summary>描画対象文字列の文字数</summary>
    public int TextLength;

    /// <summary>配置基準</summary>
    public TextAlignment Alignment;

    private byte _padding0;
    private byte _padding1;
    private byte _padding2;
}

/// <summary>
///     描画ブレンドモード設定コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SetBlendModePacket {
    /// <summary>ブレンドモード値</summary>
    public int Mode;

    /// <summary>ゼロから255の範囲で指定するブレンドアルファ値</summary>
    public int Pal;
}

/// <summary>
///     描画輝度設定コマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SetBrightPacket {
    /// <summary>ゼロから255の範囲で指定する赤成分輝度</summary>
    public int R;

    /// <summary>ゼロから255の範囲で指定する緑成分輝度</summary>
    public int G;

    /// <summary>ゼロから255の範囲で指定する青成分輝度</summary>
    public int B;
}

/// <summary>
///     円描画バッチコマンドデータ。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct DrawCircleBatchPacket {
    /// <summary>CompactCirclePacket バッファ内の先頭インデックス</summary>
    public int Offset;

    /// <summary>描画する円の要素数</summary>
    public int Count;
}

/// <summary>
///     言語境界を越えて受け渡す固定長描画コマンドパケットです。
///     アンマネージドメモリとのゼロコピー転送を実現するため48バイト均一サイズにアライメントしています。
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct RenderPacket {
    /// <summary>コマンド種別</summary>
    [FieldOffset(0)] public RenderPacketType Type;

    /// <summary>アライメント調整用の予約領域</summary>
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

    /// <summary>高密度円描画バッチコマンドデータ</summary>
    [FieldOffset(4)] public DrawCircleBatchPacket CircleBatch;
}