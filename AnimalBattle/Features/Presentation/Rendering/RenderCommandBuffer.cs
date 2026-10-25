using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AnimalBattle.Bindings;
using AnimalBattle.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     64バイト境界に整列された単一アンマネージドメモリ上で描画コマンドを蓄積し、ネイティブエンジンへ一括転送するコマンドバッファ。
/// </summary>
public sealed unsafe class RenderCommandBuffer : IDisposable {
    private const int DefaultPacketCapacity = 16384;
    private const int DefaultTextCapacity = 8192;

    // 64バイト整列された単一アンマネージド描画バッファ
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

    // 冗長な状態変更コマンドを省くシャドウキャッシュ
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

    private bool _isDisposed;

    /// <summary>
    ///     指定された初期容量で64バイト整列のアンマネージドメモリを確保し、コマンドバッファを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンド容量</param>
    /// <param name="initialTextCapacity">初期テキスト容量</param>
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
        _isDisposed = false;

        var packetBytes = (nuint)(sizeof(RenderPacket) * _packetCapacity);
        var textBytes = (nuint)(sizeof(char) * _textCapacity);
        var circleBytes = (nuint)(sizeof(CompactCirclePacket) * _circleCapacity);

        _packetBuffer = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes, 64);
        _textBuffer = (char*)NativeMemory.AlignedAlloc(textBytes, 64);
        _circleBuffer = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes, 64);
    }

    /// <summary>
    ///     アンマネージドメモリを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~RenderCommandBuffer()
    {
        Dispose(false);
    }

    /// <summary>
    ///     カウンタと描画状態キャッシュをリセットし、新規フレームの記録を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRecording()
    {
        Debug.Assert(!_isDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対する操作です。");
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

    /// <summary>
    ///     矩形描画コマンドを追加します。
    /// </summary>
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
    ///     円描画コマンドを20バイト緊密配置バッファへ直接追加します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushDrawCircle(Vec2i center, int radius, uint color, bool fill)
    {
        EnsureCircleCapacity(1);
        _circleBuffer[_currentCircleCount++] = new CompactCirclePacket(center, radius, color, fill);
    }

    /// <summary>
    ///     緊密配置された円描画コマンド群を一括追加します。
    /// </summary>
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
                    (long)(sizeof(CompactCirclePacket) * (_circleCapacity - _currentCircleCount)),
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
    ///     線分描画コマンドを追加します。
    /// </summary>
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
    ///     画像描画コマンドを追加します。
    /// </summary>
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
    ///     回転拡縮画像描画コマンドを追加します。
    /// </summary>
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
    ///     矩形切り出し回転拡縮画像描画コマンドを追加します。
    /// </summary>
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
    ///     文字列描画コマンドを追加します。
    /// </summary>
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
    ///     描画ブレンドモード設定コマンドを追加します（同一状態の場合はスキップ）。
    /// </summary>
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
    ///     描画輝度設定コマンドを追加します（同一状態の場合はスキップ）。
    /// </summary>
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
    ///     蓄積された描画コマンドをネイティブエンジンへ一括提出します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Submit()
    {
        Debug.Assert(!_isDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対する操作です。");
        if (_isDisposed) return;
        EnsurePendingCircleBatchFlushed();

        if (_currentPacketCount > 0 || _currentCircleCount > 0)
        {
            NativeBindings.Engine_SubmitRenderCommands(
                _packetBuffer,
                _currentPacketCount,
                _textBuffer,
                _currentTextLength,
                _circleBuffer,
                _currentCircleCount);
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

    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;

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

        _isDisposed = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePacketCapacity(int extraCount)
    {
        Debug.Assert(!_isDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対する操作です。");
        var required = _currentPacketCount + extraCount;
        if (required <= _packetCapacity) return;

        var newCapacity = Math.Max(_packetCapacity * 2, required);
        var byteCount = (nuint)(sizeof(RenderPacket) * newCapacity);

        _packetBuffer = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer, byteCount, 64);
        _packetCapacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureTextCapacity(int extraLength)
    {
        Debug.Assert(!_isDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対する操作です。");
        var required = _currentTextLength + extraLength;
        if (required <= _textCapacity) return;

        var newCapacity = Math.Max(_textCapacity * 2, required);
        var byteCount = (nuint)(sizeof(char) * newCapacity);

        _textBuffer = (char*)NativeMemory.AlignedRealloc(_textBuffer, byteCount, 64);
        _textCapacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCircleCapacity(int extraCount)
    {
        Debug.Assert(!_isDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対する操作です。");
        var required = _currentCircleCount + extraCount;
        if (required <= _circleCapacity) return;

        var newCapacity = Math.Max(_circleCapacity * 2, required);
        var byteCount = (nuint)(sizeof(CompactCirclePacket) * newCapacity);

        _circleBuffer = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer, byteCount, 64);
        _circleCapacity = newCapacity;
    }
}
