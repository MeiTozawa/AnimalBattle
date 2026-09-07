using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;

namespace AnimalBattle.Render;

/// <summary>
/// <see cref="NativeMemory.AlignedAlloc"/> により 64 バイト Cache Line 境界に整列された非托管連続物理メモリ上で
/// 1 フレーム内の描画コマンドおよび文字列データを蓄積し、C++ コアエンジンへ一括転送する零 GC 描画指令バッファ。
/// fixed ピン留め不要・GC アロケーションゼロで動作し、<see cref="IDisposable"/> による厳格な RAII 寿命管理を提供します。
/// </summary>
public unsafe sealed class RenderCommandBuffer : IDisposable
{
    private const int _defaultPacketCapacity = 4096;
    private const int _defaultTextCapacity = 4096;

    private RenderPacket* _packetBuffer;
    private int _packetCapacity;
    private int _packetCount;

    private char* _textBuffer;
    private int _textCapacity;
    private int _textLength;

    private bool _isDisposed;

    /// <summary>
    /// 現在バッファに格納されている描画コマンド数。
    /// </summary>
    public int PacketCount => _packetCount;

    /// <summary>
    /// 現在バッファに格納されている文字列バッファの文字数。
    /// </summary>
    public int TextLength => _textLength;

    /// <summary>
    /// 描画コマンドバッファの最大収容可能要素数。
    /// </summary>
    public int PacketCapacity => _packetCapacity;

    /// <summary>
    /// 文字列バッファの最大収容可能文字数。
    /// </summary>
    public int TextCapacity => _textCapacity;

    /// <summary>
    /// リソースが破棄済みであるか否かを取得します。
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// 描画コマンドバッファの先頭物理アドレス（アライメント検証用）。
    /// </summary>
    internal void* PacketBufferPtr => _packetBuffer;

    /// <summary>
    /// 文字列バッファの先頭物理アドレス（アライメント検証用）。
    /// </summary>
    internal void* TextBufferPtr => _textBuffer;

    /// <summary>
    /// 指定された初期容量で 64 バイト物理整列の非托管メモリを確保し、描画コマンドバッファを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンドバッファ容量。</param>
    /// <param name="initialTextCapacity">初期テキストバッファ容量。</param>
    public RenderCommandBuffer(int initialPacketCapacity = _defaultPacketCapacity, int initialTextCapacity = _defaultTextCapacity)
    {
        _packetCapacity = Math.Max(64, initialPacketCapacity);
        _textCapacity = Math.Max(64, initialTextCapacity);
        _packetCount = 0;
        _textLength = 0;
        _isDisposed = false;

        nuint packetBytes = (nuint)(sizeof(RenderPacket) * _packetCapacity);
        nuint textBytes = (nuint)(sizeof(char) * _textCapacity);

        _packetBuffer = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes, 64);
        _textBuffer = (char*)NativeMemory.AlignedAlloc(textBytes, 64);
    }

    /// <summary>
    /// アンマネージドリソースの解放漏れを防ぐファイナライザ。
    /// </summary>
    ~RenderCommandBuffer()
    {
        Dispose(false);
    }

    /// <summary>
    /// 矩形描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x1">左上 X 座標。</param>
    /// <param name="y1">左上 Y 座標。</param>
    /// <param name="x2">右下 X 座標。</param>
    /// <param name="y2">右下 Y 座標。</param>
    /// <param name="color">描画カラー。</param>
    /// <param name="fill">塗りつぶしフラグ。</param>
    public void PushDrawBox(int x1, int y1, int x2, int y2, uint color, bool fill)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawBox,
            Box = new()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Color = color,
                Fill = fill ? (byte)1 : (byte)0,
            },
        };
    }

    /// <summary>
    /// 円描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標。</param>
    /// <param name="centerY">中心 Y 座標。</param>
    /// <param name="radius">半径 (px)。</param>
    /// <param name="color">描画カラー。</param>
    /// <param name="fill">塗りつぶしフラグ。</param>
    public void PushDrawCircle(int centerX, int centerY, int radius, uint color, bool fill)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawCircle,
            Circle = new()
            {
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius,
                Color = color,
                Fill = fill ? (byte)1 : (byte)0,
            },
        };
    }

    /// <summary>
    /// 線分描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x1">始点 X 座標。</param>
    /// <param name="y1">始点 Y 座標。</param>
    /// <param name="x2">終点 X 座標。</param>
    /// <param name="y2">終点 Y 座標。</param>
    /// <param name="color">描画カラー。</param>
    /// <param name="thickness">線の太さ。</param>
    public void PushDrawLine(int x1, int y1, int x2, int y2, uint color, int thickness)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawLine,
            Line = new()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Color = color,
                Thickness = thickness,
            },
        };
    }

    /// <summary>
    /// 画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標。</param>
    /// <param name="y">描画先 Y 座標。</param>
    /// <param name="handle">画像ハンドル。</param>
    /// <param name="trans">透過フラグ。</param>
    public void PushDrawGraph(int x, int y, int handle, bool trans)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawGraph,
            Graph = new()
            {
                X = x,
                Y = y,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0,
            },
        };
    }

    /// <summary>
    /// 回転拡縮画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標。</param>
    /// <param name="centerY">中心 Y 座標。</param>
    /// <param name="scale">拡大率。</param>
    /// <param name="angle">回転角度 (ラジアン)。</param>
    /// <param name="handle">画像ハンドル。</param>
    /// <param name="trans">透過フラグ。</param>
    public void PushDrawRotaGraph(int centerX, int centerY, float scale, float angle, int handle, bool trans)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawRotaGraph,
            RotaGraph = new()
            {
                CenterX = centerX,
                CenterY = centerY,
                Scale = scale,
                Angle = angle,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0,
            },
        };
    }

    /// <summary>
    /// 矩形切り出し回転拡縮画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標。</param>
    /// <param name="centerY">中心 Y 座標。</param>
    /// <param name="srcX">切り出し開始 X 座標。</param>
    /// <param name="srcY">切り出し開始 Y 座標。</param>
    /// <param name="srcW">切り出し幅。</param>
    /// <param name="srcH">切り出し高さ。</param>
    /// <param name="scale">拡大率。</param>
    /// <param name="angle">回転角度 (ラジアン)。</param>
    /// <param name="handle">画像ハンドル。</param>
    /// <param name="trans">透過フラグ。</param>
    /// <param name="turnX">左右反転フラグ。</param>
    public void PushDrawRectRotaGraph(
        int centerX, int centerY,
        int srcX, int srcY, int srcW, int srcH,
        float scale, float angle,
        int handle, bool trans, bool turnX)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawRectRotaGraph,
            RectRotaGraph = new()
            {
                CenterX = centerX,
                CenterY = centerY,
                SrcX = srcX,
                SrcY = srcY,
                SrcW = srcW,
                SrcH = srcH,
                Scale = scale,
                Angle = angle,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0,
                TurnX = turnX ? (byte)1 : (byte)0,
            },
        };
    }

    /// <summary>
    /// 文字列描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標。</param>
    /// <param name="y">描画先 Y 座標。</param>
    /// <param name="text">描画文字列。</param>
    /// <param name="color">描画カラー。</param>
    public void PushDrawString(int x, int y, ReadOnlySpan<char> text, uint color)
    {
        EnsurePacketCapacity(1);
        int strLength = text.Length;
        EnsureTextCapacity(strLength + 1);
        int offset = _textLength;

        Span<char> destination = new(_textBuffer + offset, strLength + 1);
        text.CopyTo(destination);
        _textBuffer[offset + strLength] = '\0';
        _textLength += strLength + 1;

        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.DrawString,
            StringData = new()
            {
                X = x,
                Y = y,
                Color = color,
                TextOffset = offset,
                TextLength = strLength,
            },
        };
    }

    /// <summary>
    /// 描画ブレンドモード設定コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="mode">ブレンドモード値。</param>
    /// <param name="pal">ブレンドパラメータ。</param>
    public void PushSetDrawBlendMode(int mode, int pal)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.SetDrawBlendMode,
            Blend = new()
            {
                Mode = mode,
                Pal = pal,
            },
        };
    }

    /// <summary>
    /// 描画輝度設定コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="r">赤輝度。</param>
    /// <param name="g">緑輝度。</param>
    /// <param name="b">青輝度。</param>
    public void PushSetDrawBright(int r, int g, int b)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[_packetCount++] = new()
        {
            Type = RenderPacketType.SetDrawBright,
            Bright = new()
            {
                R = r,
                G = g,
                B = b,
            },
        };
    }

    /// <summary>
    /// 蓄積された描画コマンドを 1 回の P/Invoke で C++ ネイティブエンジンへ一括提出します。
    /// fixed ピン留めを行わず、Cache Line 整列済みの非托管物理ポインタを直接渡します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_packetCount <= 0)
        {
            return;
        }

        NativeBindings.Engine_SubmitRenderCommands(_packetBuffer, _packetCount, _textBuffer, _textLength);
    }

    /// <summary>
    /// 描画バッファのカウンターをリセットし、次フレームの記録に備えます（GC 発生なし）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _packetCount = 0;
        _textLength = 0;
    }

    /// <summary>
    /// 確保されている非托管物理メモリを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_packetBuffer != null)
        {
            NativeMemory.AlignedFree(_packetBuffer);
            _packetBuffer = null;
        }

        if (_textBuffer != null)
        {
            NativeMemory.AlignedFree(_textBuffer);
            _textBuffer = null;
        }

        _isDisposed = true;
    }

    private void EnsurePacketCapacity(int extraCount)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        int required = _packetCount + extraCount;
        if (required > _packetCapacity)
        {
            int newCapacity = Math.Max(_packetCapacity * 2, required);
            nuint byteCount = (nuint)(sizeof(RenderPacket) * newCapacity);
            _packetBuffer = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer, byteCount, 64);
            _packetCapacity = newCapacity;
        }
    }

    private void EnsureTextCapacity(int extraLength)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        int required = _textLength + extraLength;
        if (required > _textCapacity)
        {
            int newCapacity = Math.Max(_textCapacity * 2, required);
            nuint byteCount = (nuint)(sizeof(char) * newCapacity);
            _textBuffer = (char*)NativeMemory.AlignedRealloc(_textBuffer, byteCount, 64);
            _textCapacity = newCapacity;
        }
    }
}
