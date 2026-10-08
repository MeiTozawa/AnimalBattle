using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;

namespace AnimalBattle.Render;

/// <summary>
///     <see cref="NativeMemory.AlignedAlloc" /> により 64 バイト Cache Line 境界に整列された非托管連続物理メモリ上で
///     1 フレーム内の描画コマンドおよび文字列データを蓄積し、C++ コアエンジンへ一括転送する零 GC 描画指令バッファ。
///     fixed ピン留め不要・GC アロケーションゼロで動作し、<see cref="IDisposable" /> による厳格な RAII 寿命管理を提供します。
/// </summary>
public sealed unsafe class RenderCommandBuffer : IDisposable {
    private const int DefaultPacketCapacity = 8192;
    private const int DefaultTextCapacity = 8192;

    // 前・後台ダブルバッファの物理ポインタ
    private RenderPacket* _packetBuffer0;
    private RenderPacket* _packetBuffer1;
    private char* _textBuffer0;
    private char* _textBuffer1;
    private CompactCirclePacket* _circleBuffer0;
    private CompactCirclePacket* _circleBuffer1;

    // 現在のフレームで書き込み対象となっているアクティブバッファへのポインタ
    private RenderPacket* _packetBuffer;
    private char* _textBuffer;
    private CompactCirclePacket* _circleBuffer;
    private int _writeIndex;

    // 描画パイプライン状態シャドウキャッシュ（冗長な状態設定コマンドを排除し、GPU パイプラインストールを防止）
    private const int DefaultBlendMode = 0;
    private const int DefaultBlendPal = 0;
    private const int DefaultBrightR = 255;
    private const int DefaultBrightG = 255;
    private const int DefaultBrightB = 255;

    private int _activeBlendMode = DefaultBlendMode;
    private int _activeBlendPal = DefaultBlendPal;
    private int _activeBrightR = DefaultBrightR;
    private int _activeBrightG = DefaultBrightG;
    private int _activeBrightB = DefaultBrightB;

    /// <summary>
    ///     指定された初期容量で 64 バイト物理整列のダブルバッファ非托管メモリを確保し、描画コマンドバッファを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンドバッファ容量。</param>
    /// <param name="initialTextCapacity">初期テキストバッファ容量。</param>
    public RenderCommandBuffer(int initialPacketCapacity = DefaultPacketCapacity,
        int initialTextCapacity = DefaultTextCapacity)
    {
        PacketCapacity = Math.Max(64, initialPacketCapacity);
        TextCapacity = Math.Max(64, initialTextCapacity);
        CircleCapacity = Math.Max(64, initialPacketCapacity);
        PacketCount = 0;
        TextLength = 0;
        CircleCount = 0;
        IsDisposed = false;
        _writeIndex = 0;

        var packetBytes = (nuint)(sizeof(RenderPacket) * PacketCapacity);
        var textBytes = (nuint)(sizeof(char) * TextCapacity);
        var circleBytes = (nuint)(sizeof(CompactCirclePacket) * CircleCapacity);

        _packetBuffer0 = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes, 64);
        _packetBuffer1 = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes, 64);
        _textBuffer0 = (char*)NativeMemory.AlignedAlloc(textBytes, 64);
        _textBuffer1 = (char*)NativeMemory.AlignedAlloc(textBytes, 64);
        _circleBuffer0 = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes, 64);
        _circleBuffer1 = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes, 64);

        _packetBuffer = _packetBuffer0;
        _textBuffer = _textBuffer0;
        _circleBuffer = _circleBuffer0;
    }

    /// <summary>
    ///     現在バッファに格納されている緊密配置円コマンド数。
    /// </summary>
    public int CircleCount { get; private set; }

    /// <summary>
    ///     緊密配置円バッファの最大収容可能要素数。
    /// </summary>
    public int CircleCapacity { get; private set; }

    /// <summary>
    ///     現在バッファに格納されている描画コマンド数。
    /// </summary>
    private int PacketCount { get; set; }

    /// <summary>
    ///     現在バッファに格納されている文字列バッファの文字数。
    /// </summary>
    private int TextLength { get; set; }

    /// <summary>
    ///     描画コマンドバッファの最大収容可能要素数。
    /// </summary>
    private int PacketCapacity { get; set; }

    /// <summary>
    ///     文字列バッファの最大収容可能文字数。
    /// </summary>
    private int TextCapacity { get; set; }

    /// <summary>
    ///     リソースが破棄済みであるか否かを取得します。
    /// </summary>
    private bool IsDisposed { get; set; }

    /// <summary>
    ///     描画コマンドバッファの先頭物理アドレス（アライメント検証用）。
    /// </summary>
    internal void* PacketBufferPtr => _packetBuffer;

    /// <summary>
    ///     文字列バッファの先頭物理アドレス（アライメント検証用）。
    /// </summary>
    internal void* TextBufferPtr => _textBuffer;

    /// <summary>
    ///     高密度円コマンドバッファの先頭物理アドレス（アライメント検証用）。
    /// </summary>
    internal void* CircleBufferPtr => _circleBuffer;

    /// <summary>
    ///     確保されている非托管物理メモリを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     アンマネージドリソースの解放漏れを防ぐファイナライザ。
    /// </summary>
    ~RenderCommandBuffer()
    {
        Dispose(false);
    }

    /// <summary>
    ///     矩形描画コマンドをバッファへ追加します。
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
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawBox,
            Box = new DrawBoxPacket {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Color = color,
                Fill = fill ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     円描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="centerX">中心 X 座標。</param>
    /// <param name="centerY">中心 Y 座標。</param>
    /// <param name="radius">半径 (px)。</param>
    /// <param name="color">描画カラー。</param>
    /// <param name="fill">塗りつぶしフラグ。</param>
    public void PushDrawCircle(int centerX, int centerY, int radius, uint color, bool fill)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawCircle,
            Circle = new DrawCirclePacket {
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius,
                Color = color,
                Fill = fill ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     線分描画コマンドをバッファへ追加します。
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
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawLine,
            Line = new DrawLinePacket {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Color = color,
                Thickness = thickness
            }
        };
    }

    /// <summary>
    ///     画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標。</param>
    /// <param name="y">描画先 Y 座標。</param>
    /// <param name="handle">画像ハンドル。</param>
    /// <param name="trans">透過フラグ。</param>
    public void PushDrawGraph(int x, int y, int handle, bool trans)
    {
        EnsurePacketCapacity(1);
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawGraph,
            Graph = new DrawGraphPacket {
                X = x,
                Y = y,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     回転拡縮画像描画コマンドをバッファへ追加します。
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
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawRotaGraph,
            RotaGraph = new DrawRotaGraphPacket {
                CenterX = centerX,
                CenterY = centerY,
                Scale = scale,
                Angle = angle,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     矩形切り出し回転拡縮画像描画コマンドをバッファへ追加します。
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
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawRectRotaGraph,
            RectRotaGraph = new DrawRectRotaGraphPacket {
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
                TurnX = turnX ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     文字列描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="x">描画先 X 座標。</param>
    /// <param name="y">描画先 Y 座標。</param>
    /// <param name="text">描画文字列。</param>
    /// <param name="color">描画カラー。</param>
    public void PushDrawString(int x, int y, ReadOnlySpan<char> text, uint color)
    {
        EnsurePacketCapacity(1);
        var strLength = text.Length;
        EnsureTextCapacity(strLength + 1);
        var offset = TextLength;

        Span<char> destination = new(_textBuffer + offset, strLength + 1);
        text.CopyTo(destination);
        _textBuffer[offset + strLength] = '\0';
        TextLength += strLength + 1;

        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawString,
            StringData = new DrawStringPacket {
                X = x,
                Y = y,
                Color = color,
                TextOffset = offset,
                TextLength = strLength
            }
        };
    }

    /// <summary>
    ///     描画ブレンドモード設定コマンドをバッファへ追加します。
    ///     現在のシャドウ状態と完全に一致する場合は、冗長なコマンド送信を早期リターンで排除します。
    /// </summary>
    /// <param name="mode">ブレンドモード値。</param>
    /// <param name="pal">ブレンドパラメータ。</param>
    public void PushSetDrawBlendMode(int mode, int pal)
    {
        if (_activeBlendMode == mode && _activeBlendPal == pal) return;
        _activeBlendMode = mode;
        _activeBlendPal = pal;

        EnsurePacketCapacity(1);
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.SetDrawBlendMode,
            Blend = new SetBlendModePacket {
                Mode = mode,
                Pal = pal
            }
        };
    }

    /// <summary>
    ///     描画輝度設定コマンドをバッファへ追加します。
    ///     現在のシャドウ状態と完全に一致する場合は、冗长なコマンド送信を早期リターンで排除します。
    /// </summary>
    /// <param name="r">赤輝度。</param>
    /// <param name="g">緑輝度。</param>
    /// <param name="b">青輝度。</param>
    public void PushSetDrawBright(int r, int g, int b)
    {
        if (_activeBrightR == r && _activeBrightG == g && _activeBrightB == b) return;
        _activeBrightR = r;
        _activeBrightG = g;
        _activeBrightB = b;

        EnsurePacketCapacity(1);
        _packetBuffer[PacketCount++] = new RenderPacket {
            Type = RenderPacketType.SetDrawBright,
            Bright = new SetBrightPacket {
                R = r,
                G = g,
                B = b
            }
        };
    }

    /// <summary>
    ///     蓄積された描画コマンドを 1 回の P/Invoke で C++ ネイティブエンジンへ一括提出します。
    ///     fixed ピン留めを行わず、Cache Line 整列済みの非托管物理ポインタを直接渡します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (CircleCount > 0)
        {
            NativeBindings.Engine_SubmitCompactCircles(_circleBuffer, CircleCount);
        }

        if (PacketCount > 0)
        {
            NativeBindings.Engine_SubmitRenderCommands(_packetBuffer, PacketCount, _textBuffer, TextLength);
        }
    }

    /// <summary>
    ///     20 バイト緊密配置の高密度円描画コマンドをバッファへ追加します（48 バイト汎用パケットをバイパス）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushCompactCircle(int x, int y, int radius, uint color, bool fill)
    {
        EnsureCircleCapacity(1);
        _circleBuffer[CircleCount++] = new CompactCirclePacket(x, y, radius, color, fill);
    }

    /// <summary>
    ///     高密度円描画コマンド群をバッチメモリコピーで一括追加します。
    /// </summary>
    public void PushCompactCirclesBatch(ReadOnlySpan<CompactCirclePacket> circles)
    {
        if (circles.IsEmpty) return;
        EnsureCircleCapacity(circles.Length);
        fixed (CompactCirclePacket* src = circles)
        {
            Buffer.MemoryCopy(src, _circleBuffer + CircleCount, (long)(sizeof(CompactCirclePacket) * (CircleCapacity - CircleCount)), (long)(sizeof(CompactCirclePacket) * circles.Length));
        }
        CircleCount += circles.Length;
    }

    /// <summary>
    ///     ダブルバッファの書き込み対象インデックス (0 または 1)。
    /// </summary>
    public int WriteIndex => _writeIndex;

    /// <summary>
    ///     前・後台バッファを乒乓（Ping-Pong）切り替えし、次フレームの記録バッファを準備します（GC 発生なし）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _writeIndex ^= 1;
        _packetBuffer = _writeIndex == 0 ? _packetBuffer0 : _packetBuffer1;
        _textBuffer = _writeIndex == 0 ? _textBuffer0 : _textBuffer1;
        _circleBuffer = _writeIndex == 0 ? _circleBuffer0 : _circleBuffer1;
        PacketCount = 0;
        TextLength = 0;
        CircleCount = 0;
        _activeBlendMode = DefaultBlendMode;
        _activeBlendPal = DefaultBlendPal;
        _activeBrightR = DefaultBrightR;
        _activeBrightG = DefaultBrightG;
        _activeBrightB = DefaultBrightB;
    }

    /// <summary>
    ///     描画バッファのカウンターおよびパイプラインシャドウ状態をリセットし、次フレームの記録に備えます（GC 発生なし）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        Swap();
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (_packetBuffer0 != null)
        {
            NativeMemory.AlignedFree(_packetBuffer0);
            _packetBuffer0 = null;
        }

        if (_packetBuffer1 != null)
        {
            NativeMemory.AlignedFree(_packetBuffer1);
            _packetBuffer1 = null;
        }

        if (_textBuffer0 != null)
        {
            NativeMemory.AlignedFree(_textBuffer0);
            _textBuffer0 = null;
        }

        if (_textBuffer1 != null)
        {
            NativeMemory.AlignedFree(_textBuffer1);
            _textBuffer1 = null;
        }

        if (_circleBuffer0 != null)
        {
            NativeMemory.AlignedFree(_circleBuffer0);
            _circleBuffer0 = null;
        }

        if (_circleBuffer1 != null)
        {
            NativeMemory.AlignedFree(_circleBuffer1);
            _circleBuffer1 = null;
        }

        _packetBuffer = null;
        _textBuffer = null;
        _circleBuffer = null;

        IsDisposed = true;
    }

    private void EnsurePacketCapacity(int extraCount)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var required = PacketCount + extraCount;
        if (required <= PacketCapacity) return;
        var newCapacity = Math.Max(PacketCapacity * 2, required);
        var byteCount = (nuint)(sizeof(RenderPacket) * newCapacity);
        _packetBuffer0 = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer0, byteCount, 64);
        _packetBuffer1 = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer1, byteCount, 64);
        _packetBuffer = _writeIndex == 0 ? _packetBuffer0 : _packetBuffer1;
        PacketCapacity = newCapacity;
    }

    private void EnsureTextCapacity(int extraLength)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var required = TextLength + extraLength;
        if (required > TextCapacity)
        {
            var newCapacity = Math.Max(TextCapacity * 2, required);
            var byteCount = (nuint)(sizeof(char) * newCapacity);
            _textBuffer0 = (char*)NativeMemory.AlignedRealloc(_textBuffer0, byteCount, 64);
            _textBuffer1 = (char*)NativeMemory.AlignedRealloc(_textBuffer1, byteCount, 64);
            _textBuffer = _writeIndex == 0 ? _textBuffer0 : _textBuffer1;
            TextCapacity = newCapacity;
        }
    }

    private void EnsureCircleCapacity(int extraCount)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var required = CircleCount + extraCount;
        if (required <= CircleCapacity) return;
        var newCapacity = Math.Max(CircleCapacity * 2, required);
        var byteCount = (nuint)(sizeof(CompactCirclePacket) * newCapacity);
        _circleBuffer0 = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer0, byteCount, 64);
        _circleBuffer1 = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer1, byteCount, 64);
        _circleBuffer = _writeIndex == 0 ? _circleBuffer0 : _circleBuffer1;
        CircleCapacity = newCapacity;
    }
}