using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Render;

/// <summary>
/// 1 フレーム内の描画コマンドおよびテキストデータを蓄積し、C++ へ一括転送する零 GC 描画指令バッファ。
/// </summary>
public sealed class RenderCommandBuffer
{
    private const int _initialPacketCapacity = 4096;
    private const int _initialTextCapacity = 4096;

    private RenderPacket[] _packets;
    private int _packetCount;

    private char[] _textBuffer;
    private int _textLength;

    /// <summary>
    /// 現在バッファに格納されている描画コマンド数。
    /// </summary>
    public int PacketCount => _packetCount;

    /// <summary>
    /// 現在バッファに格納されている文字列バッファの文字数。
    /// </summary>
    public int TextLength => _textLength;

    /// <summary>
    /// <see cref="RenderCommandBuffer"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンド配列容量</param>
    /// <param name="initialTextCapacity">初期テキスト配列容量</param>
    public RenderCommandBuffer(int initialPacketCapacity = _initialPacketCapacity, int initialTextCapacity = _initialTextCapacity)
    {
        _packets = new RenderPacket[Math.Max(64, initialPacketCapacity)];
        _textBuffer = new char[Math.Max(64, initialTextCapacity)];
        _packetCount = 0;
        _textLength = 0;
    }

    /// <summary>
    /// 矩形描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x1">左上 X 座標</param>
    /// <param name="y1">左上 Y 座標</param>
    /// <param name="x2">右下 X 座標</param>
    /// <param name="y2">右下 Y 座標</param>
    /// <param name="color">描画カラー</param>
    /// <param name="fill">塗りつぶしフラグ</param>
    public void PushDrawBox(int x1, int y1, int x2, int y2, uint color, bool fill)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawBox,
            BoxX1 = x1,
            BoxY1 = y1,
            BoxX2 = x2,
            BoxY2 = y2,
            BoxColor = color,
            BoxFill = fill ? (byte)1 : (byte)0,
        };
    }

    /// <summary>
    /// 円描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標</param>
    /// <param name="centerY">中心 Y 座標</param>
    /// <param name="radius">半径 (px)</param>
    /// <param name="color">描画カラー</param>
    /// <param name="fill">塗りつぶしフラグ</param>
    public void PushDrawCircle(int centerX, int centerY, int radius, uint color, bool fill)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawCircle,
            CircleCenterX = centerX,
            CircleCenterY = centerY,
            CircleRadius = radius,
            CircleColor = color,
            CircleFill = fill ? (byte)1 : (byte)0,
        };
    }

    /// <summary>
    /// 線分描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x1">始点 X 座標</param>
    /// <param name="y1">始点 Y 座標</param>
    /// <param name="x2">終点 X 座標</param>
    /// <param name="y2">終点 Y 座標</param>
    /// <param name="color">描画カラー</param>
    /// <param name="thickness">線の太さ</param>
    public void PushDrawLine(int x1, int y1, int x2, int y2, uint color, int thickness)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawLine,
            LineX1 = x1,
            LineY1 = y1,
            LineX2 = x2,
            LineY2 = y2,
            LineColor = color,
            LineThickness = thickness,
        };
    }

    /// <summary>
    /// 画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標</param>
    /// <param name="y">描画先 Y 座標</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    public void PushDrawGraph(int x, int y, int handle, bool trans)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawGraph,
            GraphX = x,
            GraphY = y,
            GraphHandle = handle,
            GraphTrans = trans ? (byte)1 : (byte)0,
        };
    }

    /// <summary>
    /// 回転拡縮画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標</param>
    /// <param name="centerY">中心 Y 座標</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    public void PushDrawRotaGraph(int centerX, int centerY, float scale, float angle, int handle, bool trans)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawRotaGraph,
            RotaCenterX = centerX,
            RotaCenterY = centerY,
            RotaScale = scale,
            RotaAngle = angle,
            RotaHandle = handle,
            RotaTrans = trans ? (byte)1 : (byte)0,
        };
    }

    /// <summary>
    /// 矩形切り出し回転拡縮画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標</param>
    /// <param name="centerY">中心 Y 座標</param>
    /// <param name="srcX">切り出し開始 X 座標</param>
    /// <param name="srcY">切り出し開始 Y 座標</param>
    /// <param name="srcW">切り出し幅</param>
    /// <param name="srcH">切り出し高さ</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    /// <param name="turnX">左右反転フラグ</param>
    public void PushDrawRectRotaGraph(
        int centerX, int centerY,
        int srcX, int srcY, int srcW, int srcH,
        float scale, float angle,
        int handle, bool trans, bool turnX)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawRectRotaGraph,
            RectRotaCenterX = centerX,
            RectRotaCenterY = centerY,
            RectRotaSrcX = srcX,
            RectRotaSrcY = srcY,
            RectRotaSrcW = srcW,
            RectRotaSrcH = srcH,
            RectRotaScale = scale,
            RectRotaAngle = angle,
            RectRotaHandle = handle,
            RectRotaTrans = trans ? (byte)1 : (byte)0,
            RectRotaTurnX = turnX ? (byte)1 : (byte)0,
        };
    }

    /// <summary>
    /// 文字列描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標</param>
    /// <param name="y">描画先 Y 座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー</param>
    public void PushDrawString(int x, int y, ReadOnlySpan<char> text, uint color)
    {
        int strLength = text.Length;
        EnsureTextCapacity(strLength + 1);
        int offset = _textLength;

        text.CopyTo(_textBuffer.AsSpan(offset, strLength));
        _textBuffer[offset + strLength] = '\0';
        _textLength += strLength + 1;

        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.DrawString,
            StringX = x,
            StringY = y,
            StringColor = color,
            StringOffset = offset,
            StringLength = strLength,
        };
    }

    /// <summary>
    /// 描画ブレンドモード設定コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="mode">ブレンドモード値</param>
    /// <param name="pal">ブレンドパラメータ</param>
    public void PushSetDrawBlendMode(int mode, int pal)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.SetDrawBlendMode,
            BlendMode = mode,
            BlendPal = pal,
        };
    }

    /// <summary>
    /// 描画輝度設定コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="r">赤輝度</param>
    /// <param name="g">緑輝度</param>
    /// <param name="b">青輝度</param>
    public void PushSetDrawBright(int r, int g, int b)
    {
        EnsurePacketCapacity(1);
        _packets[_packetCount++] = new RenderPacket
        {
            Type = RenderPacketType.SetDrawBright,
            BrightR = r,
            BrightG = g,
            BrightB = b,
        };
    }

    /// <summary>
    /// 蓄積された描画コマンドを 1 回の P/Invoke で C++ ネイティブエンジンへ一括提出します。
    /// </summary>
    public unsafe void Submit()
    {
        if (_packetCount <= 0)
        {
            return;
        }

        fixed (RenderPacket* pPackets = _packets)
        fixed (char* pText = _textBuffer)
        {
            NativeBindings.Engine_SubmitRenderCommands(pPackets, _packetCount, pText, _textLength);
        }
    }

    /// <summary>
    /// 描画バッファのカウンターをリセットし、次フレームの記録に備えます（GC 発生なし）。
    /// </summary>
    public void Reset()
    {
        _packetCount = 0;
        _textLength = 0;
    }

    private void EnsurePacketCapacity(int extraCount)
    {
        int required = _packetCount + extraCount;
        if (required > _packets.Length)
        {
            int newCapacity = Math.Max(_packets.Length * 2, required);
            Array.Resize(ref _packets, newCapacity);
        }
    }

    private void EnsureTextCapacity(int extraLength)
    {
        int required = _textLength + extraLength;
        if (required > _textBuffer.Length)
        {
            int newCapacity = Math.Max(_textBuffer.Length * 2, required);
            Array.Resize(ref _textBuffer, newCapacity);
        }
    }
}
