using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;
using AnimalBattle.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     64バイトCache Line境界に整列されたアンマネージド連続物理メモリ上で1フレーム内の描画コマンドを蓄積し、コアエンジンへ一括転送する描画コマンドバッファです。
///     ガベージコレクションによるメモリ停止を完全排除し、IDisposableによる決定論的な寿命管理を提供します。
/// </summary>
public sealed unsafe class RenderCommandBuffer : IDisposable {
    private const int DefaultPacketCapacity = 16384;
    private const int DefaultTextCapacity = 8192;

    // 64バイト物理整列された単一アンマネージド描画バッファ
    private RenderPacket* _packetBuffer;
    private char* _textBuffer;
    private CompactCirclePacket* _circleBuffer;

    private int _packetCapacity;
    private int _textCapacity;
    private int _circleCapacity;

    private int _circleBatchStart;
    private int _currentPacketCount;
    private int _currentTextLength;
    private int _currentCircleCount;

    // 冗長な状態設定コマンドを排除して描画パイプラインの停止を防止するシャドウキャッシュ
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
    ///     指定された初期容量で64バイト物理整列のアンマネージドメモリを確保し、単一描画コマンドバッファを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンドバッファ容量</param>
    /// <param name="initialTextCapacity">初期テキストバッファ容量</param>
    public RenderCommandBuffer(int initialPacketCapacity = DefaultPacketCapacity,
        int initialTextCapacity = DefaultTextCapacity)
    {
        _packetCapacity = Math.Max(64, initialPacketCapacity);
        _textCapacity = Math.Max(64, initialTextCapacity);
        _circleCapacity = Math.Max(64, initialPacketCapacity);

        _currentPacketCount = 0;
        _currentTextLength = 0;
        _currentCircleCount = 0;
        _circleBatchStart = 0;
        IsDisposed = false;

        var packetBytes = (nuint)(sizeof(RenderPacket) * _packetCapacity);
        var textBytes = (nuint)(sizeof(char) * _textCapacity);
        var circleBytes = (nuint)(sizeof(CompactCirclePacket) * _circleCapacity);

        _packetBuffer = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes, 64);
        _textBuffer = (char*)NativeMemory.AlignedAlloc(textBytes, 64);
        _circleBuffer = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes, 64);
    }

    /// <summary>
    ///     現在のバッファに格納されている緊密配置円コマンド数。
    /// </summary>
    public int CircleCount => _currentCircleCount;

    /// <summary>
    ///     現在のバッファにおける円バッチの最大収容可能要素数です。
    /// </summary>
    public int CircleCapacity => _circleCapacity;

    /// <summary>
    ///     現在のバッファに格納されている描画コマンド数。
    /// </summary>
    public int PacketCount => _currentPacketCount;

    /// <summary>
    ///     現在のバッファに格納されている文字列バッファの文字数。
    /// </summary>
    public int TextLength => _currentTextLength;

    /// <summary>
    ///     現在のバッファにおける描画コマンドバッファの最大収容可能要素数です。
    /// </summary>
    public int PacketCapacity => _packetCapacity;

    /// <summary>
    ///     現在のバッファにおける文字列バッファの最大収容可能文字数です。
    /// </summary>
    public int TextCapacity => _textCapacity;

    /// <summary>
    ///     リソースが破棄済みであるか否かを取得します。
    /// </summary>
    private bool IsDisposed { get; set; }

    /// <summary>
    ///     アライメント検証用の描画コマンドバッファ先頭物理アドレスです。
    /// </summary>
    internal void* PacketBufferPtr => _packetBuffer;

    /// <summary>
    ///     アライメント検証用の文字列バッファ先頭物理アドレスです。
    /// </summary>
    internal void* TextBufferPtr => _textBuffer;

    /// <summary>
    ///     アライメント検証用の高密度円コマンドバッファ先頭物理アドレスです。
    /// </summary>
    internal void* CircleBufferPtr => _circleBuffer;

    /// <summary>
    ///     確保されているアンマネージド物理メモリを安全に解放します。
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
    /// <param name="p1">左上座標</param>
    /// <param name="p2">右下座標</param>
    /// <param name="color">描画カラー</param>
    /// <param name="fill">塗りつぶしフラグ</param>
    public void PushDrawBox(Vec2i p1, Vec2i p2, uint color, bool fill)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawBox,
            Box = new DrawBoxPacket {
                P1 = p1,
                P2 = p2,
                Color = color,
                Fill = fill ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     円描画コマンドをバッファへ追加します。20 バイトの緊密配置円バッファへ高速転送されます。
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="radius">ピクセル単位の半径</param>
    /// <param name="color">描画カラー</param>
    /// <param name="fill">塗りつぶしフラグ</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushDrawCircle(Vec2i center, int radius, uint color, bool fill)
    {
        PushCompactCircle(center, radius, color, fill);
    }

    /// <summary>
    ///     線分描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="p1">始点座標</param>
    /// <param name="p2">終点座標</param>
    /// <param name="color">描画カラー</param>
    /// <param name="thickness">線の太さ</param>
    public void PushDrawLine(Vec2i p1, Vec2i p2, uint color, int thickness)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawLine,
            Line = new DrawLinePacket {
                P1 = p1,
                P2 = p2,
                Color = color,
                Thickness = thickness
            }
        };
    }

    /// <summary>
    ///     画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="pos">描画先座標</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    public void PushDrawGraph(Vec2i pos, int handle, bool trans)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawGraph,
            Graph = new DrawGraphPacket {
                Pos = pos,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     回転拡縮画像描画コマンドをバッファへ追加します。
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">ラジアン単位の回転角度</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    public void PushDrawRotaGraph(Vec2i center, float scale, float angle, int handle, bool trans)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawRotaGraph,
            RotaGraph = new DrawRotaGraphPacket {
                Center = center,
                Scale = scale,
                Angle = angle,
                Handle = handle,
                Trans = trans ? (byte)1 : (byte)0
            }
        };
    }

    /// <summary>
    ///     独立した縦横スケール指定に対応した矩形切り出し回転拡縮画像をバッファへ追加します。
    /// </summary>
    /// <param name="center">描画中心座標</param>
    /// <param name="srcPos">切り出し開始座標</param>
    /// <param name="srcSize">切り出しサイズ</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">ラジアン単位の回転角度</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過フラグ</param>
    /// <param name="turnX">左右反転フラグ</param>
    public void PushDrawRectRotaGraph(
        Vec2i center,
        Vec2i srcPos,
        Vec2i srcSize,
        Vec2f scale,
        float angle,
        int handle,
        bool trans,
        bool turnX)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawRectRotaGraph,
            RectRotaGraph = new DrawRectRotaGraphPacket {
                Center = center,
                SrcPos = srcPos,
                SrcSize = srcSize,
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
    /// <param name="pos">描画先座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー</param>
    public void PushDrawString(Vec2i pos, ReadOnlySpan<char> text, uint color)
    {
        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        var strLength = text.Length;
        EnsureTextCapacity(strLength + 1);
        var offset = _currentTextLength;

        Span<char> destination = new(_textBuffer + offset, strLength + 1);
        text.CopyTo(destination);
        _textBuffer[offset + strLength] = '\0';
        _currentTextLength += strLength + 1;

        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.DrawString,
            StringData = new DrawStringPacket {
                Pos = pos,
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

        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
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

        EnsurePendingCircleBatchFlushed();
        EnsurePacketCapacity(1);
        _packetBuffer[_currentPacketCount++] = new RenderPacket {
            Type = RenderPacketType.SetDrawBright,
            Bright = new SetBrightPacket {
                R = r,
                G = g,
                B = b
            }
        };
    }

    /// <summary>
    ///     蓄積された描画コマンドを1回のP/InvokeでC++ネイティブエンジンへ一括提出します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        EnsurePendingCircleBatchFlushed();

        if (_currentPacketCount > 0 || _currentCircleCount > 0)
        {
            NativeBindings.Engine_SubmitRenderCommands(_packetBuffer, _currentPacketCount, _textBuffer, _currentTextLength, _circleBuffer, _currentCircleCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePendingCircleBatchFlushed()
    {
        if (_currentCircleCount > _circleBatchStart)
        {
            var count = _currentCircleCount - _circleBatchStart;
            var offset = _circleBatchStart;
            _circleBatchStart = _currentCircleCount;

            EnsurePacketCapacity(1);
            _packetBuffer[_currentPacketCount++] = new RenderPacket {
                Type = RenderPacketType.DrawCircleBatch,
                CircleBatch = new DrawCircleBatchPacket {
                    Offset = offset,
                    Count = count
                }
            };
        }
    }

    /// <summary>
    ///     48バイト汎用パケットを経由せず20バイト緊密配置の高密度円描画コマンドをバッファへ直接追加します。
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="radius">ピクセル単位の半径</param>
    /// <param name="color">描画カラー</param>
    /// <param name="fill">塗りつぶしフラグ</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushCompactCircle(Vec2i center, int radius, uint color, bool fill)
    {
        EnsureCircleCapacity(1);
        _circleBuffer[_currentCircleCount++] = new CompactCirclePacket(center, radius, color, fill);
    }

    /// <summary>
    ///     高密度円描画コマンド群を一括追加します。
    ///     オフセットがゼロの場合はメモリ一括コピーを行い、オフセットが存在する場合は
    ///     単一の容量検証後に連続メモリ上で直接座標補正書き込みを行います。
    /// </summary>
    /// <param name="circles">緊密配置された円パケットのスパン</param>
    /// <param name="offset">適用するカメラ描画オフセット</param>
    public void PushCompactCirclesBatch(ReadOnlySpan<CompactCirclePacket> circles, Vec2i offset)
    {
        if (circles.IsEmpty) return;
        EnsureCircleCapacity(circles.Length);
        fixed (CompactCirclePacket* src = circles)
        {
            var dst = _circleBuffer + _currentCircleCount;
            if (offset == Vec2i.Zero)
            {
                Buffer.MemoryCopy(
                    src,
                    dst,
                    (long)(sizeof(CompactCirclePacket) * (CircleCapacity - _currentCircleCount)),
                    (long)(sizeof(CompactCirclePacket) * circles.Length));
            }
            else
            {
                var len = circles.Length;
                for (var i = 0; i < len; i++)
                {
                    dst[i] = src[i];
                    dst[i].Center += offset;
                }
            }
        }

        _currentCircleCount += circles.Length;
    }

    /// <summary>
    ///     カメラオフセットを加算せずに高密度円描画コマンド群をバッチメモリコピーで一括追加します。
    /// </summary>
    /// <param name="circles">緊密配置された円パケットのスパン</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushCompactCirclesBatch(ReadOnlySpan<CompactCirclePacket> circles)
    {
        PushCompactCirclesBatch(circles, Vec2i.Zero);
    }

    /// <summary>
    ///     バッファのカウンターとパイプライン状態を初期化し、新規フレームの描画コマンド記録を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRecording()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        _currentPacketCount = 0;
        _currentTextLength = 0;
        _currentCircleCount = 0;
        _circleBatchStart = 0;
        _activeBlendMode = DefaultBlendMode;
        _activeBlendPal = DefaultBlendPal;
        _activeBrightR = DefaultBrightR;
        _activeBrightG = DefaultBrightG;
        _activeBrightB = DefaultBrightB;
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed) return;

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

        if (_circleBuffer != null)
        {
            NativeMemory.AlignedFree(_circleBuffer);
            _circleBuffer = null;
        }

        _packetCapacity = 0;
        _textCapacity = 0;
        _circleCapacity = 0;

        IsDisposed = true;
    }

    private void EnsurePacketCapacity(int extraCount)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentPacketCount + extraCount;
        if (required <= _packetCapacity) return;

        var newCapacity = Math.Max(_packetCapacity * 2, required);
        var byteCount = (nuint)(sizeof(RenderPacket) * newCapacity);

        _packetBuffer = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer, byteCount, 64);
        _packetCapacity = newCapacity;
    }

    private void EnsureTextCapacity(int extraLength)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentTextLength + extraLength;
        if (required <= _textCapacity) return;

        var newCapacity = Math.Max(_textCapacity * 2, required);
        var byteCount = (nuint)(sizeof(char) * newCapacity);

        _textBuffer = (char*)NativeMemory.AlignedRealloc(_textBuffer, byteCount, 64);
        _textCapacity = newCapacity;
    }

    private void EnsureCircleCapacity(int extraCount)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentCircleCount + extraCount;
        if (required <= _circleCapacity) return;

        var newCapacity = Math.Max(_circleCapacity * 2, required);
        var byteCount = (nuint)(sizeof(CompactCirclePacket) * newCapacity);

        _circleBuffer = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer, byteCount, 64);
        _circleCapacity = newCapacity;
    }
}
