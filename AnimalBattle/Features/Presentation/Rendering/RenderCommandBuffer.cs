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

    // フロントバッファとバックバッファの物理ポインタ
    private RenderPacket* _packetBuffer0;
    private RenderPacket* _packetBuffer1;
    private char* _textBuffer0;
    private char* _textBuffer1;
    private CompactCirclePacket* _circleBuffer0;
    private CompactCirclePacket* _circleBuffer1;

    // 各バッファスロットの独立要素カウント
    private int _packetCount0;
    private int _packetCount1;
    private int _textLength0;
    private int _textLength1;
    private int _circleCount0;
    private int _circleCount1;

    // スレッドセーフな独立拡縮を実現するためフロントバッファとバックバッファで容量を個別に管理します
    private int _packetCapacity0;
    private int _packetCapacity1;
    private int _textCapacity0;
    private int _textCapacity1;
    private int _circleCapacity0;
    private int _circleCapacity1;

    // 現在のフレームで書き込み対象となっているアクティブバッファへのポインタ及びアクティブカウンター
    private RenderPacket* _packetBuffer;
    private char* _textBuffer;
    private CompactCirclePacket* _circleBuffer;
    private int _writeIndex;
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
    ///     指定された初期容量で64バイト物理整列のダブルバッファアンマネージドメモリを確保し、描画コマンドバッファを初期化します。
    /// </summary>
    /// <param name="initialPacketCapacity">初期コマンドバッファ容量</param>
    /// <param name="initialTextCapacity">初期テキストバッファ容量</param>
    public RenderCommandBuffer(int initialPacketCapacity = DefaultPacketCapacity,
        int initialTextCapacity = DefaultTextCapacity)
    {
        var initPacketCap = Math.Max(64, initialPacketCapacity);
        var initTextCap = Math.Max(64, initialTextCapacity);
        var initCircleCap = Math.Max(64, initialPacketCapacity);

        _packetCapacity0 = initPacketCap;
        _packetCapacity1 = initPacketCap;
        _textCapacity0 = initTextCap;
        _textCapacity1 = initTextCap;
        _circleCapacity0 = initCircleCap;
        _circleCapacity1 = initCircleCap;

        _packetCount0 = 0;
        _packetCount1 = 0;
        _textLength0 = 0;
        _textLength1 = 0;
        _circleCount0 = 0;
        _circleCount1 = 0;
        _currentPacketCount = 0;
        _currentTextLength = 0;
        _currentCircleCount = 0;
        _circleBatchStart = 0;
        IsDisposed = false;
        _writeIndex = 0;

        var packetBytes0 = (nuint)(sizeof(RenderPacket) * _packetCapacity0);
        var packetBytes1 = (nuint)(sizeof(RenderPacket) * _packetCapacity1);
        var textBytes0 = (nuint)(sizeof(char) * _textCapacity0);
        var textBytes1 = (nuint)(sizeof(char) * _textCapacity1);
        var circleBytes0 = (nuint)(sizeof(CompactCirclePacket) * _circleCapacity0);
        var circleBytes1 = (nuint)(sizeof(CompactCirclePacket) * _circleCapacity1);

        _packetBuffer0 = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes0, 64);
        _packetBuffer1 = (RenderPacket*)NativeMemory.AlignedAlloc(packetBytes1, 64);
        _textBuffer0 = (char*)NativeMemory.AlignedAlloc(textBytes0, 64);
        _textBuffer1 = (char*)NativeMemory.AlignedAlloc(textBytes1, 64);
        _circleBuffer0 = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes0, 64);
        _circleBuffer1 = (CompactCirclePacket*)NativeMemory.AlignedAlloc(circleBytes1, 64);

        _packetBuffer = _packetBuffer0;
        _textBuffer = _textBuffer0;
        _circleBuffer = _circleBuffer0;
    }

    /// <summary>
    ///     現在アクティブな書き込みバッファに格納されている緊密配置円コマンド数。
    /// </summary>
    public int CircleCount => _currentCircleCount;

    /// <summary>
    ///     現在アクティブな書き込みバッファにおける円バッチの最大収容可能要素数です。
    /// </summary>
    public int CircleCapacity => _writeIndex == 0 ? _circleCapacity0 : _circleCapacity1;

    /// <summary>
    ///     スロット 0 の緊密配置円バッファ容量。
    /// </summary>
    public int CircleCapacity0 => _circleCapacity0;

    /// <summary>
    ///     スロット 1 の緊密配置円バッファ容量。
    /// </summary>
    public int CircleCapacity1 => _circleCapacity1;

    /// <summary>
    ///     現在アクティブな書き込みバッファに格納されている描画コマンド数。
    /// </summary>
    public int PacketCount => _currentPacketCount;

    /// <summary>
    ///     現在アクティブな書き込みバッファに格納されている文字列バッファの文字数。
    /// </summary>
    public int TextLength => _currentTextLength;

    /// <summary>
    ///     現在アクティブな書き込みバッファにおける描画コマンドバッファの最大収容可能要素数です。
    /// </summary>
    public int PacketCapacity => _writeIndex == 0 ? _packetCapacity0 : _packetCapacity1;

    /// <summary>
    ///     スロット 0 の描画コマンドバッファ容量。
    /// </summary>
    public int PacketCapacity0 => _packetCapacity0;

    /// <summary>
    ///     スロット 1 の描画コマンドバッファ容量。
    /// </summary>
    public int PacketCapacity1 => _packetCapacity1;

    /// <summary>
    ///     現在アクティブな書き込みバッファにおける文字列バッファの最大収容可能文字数です。
    /// </summary>
    public int TextCapacity => _writeIndex == 0 ? _textCapacity0 : _textCapacity1;

    /// <summary>
    ///     スロット 0 の文字列バッファ容量。
    /// </summary>
    public int TextCapacity0 => _textCapacity0;

    /// <summary>
    ///     スロット 1 の文字列バッファ容量。
    /// </summary>
    public int TextCapacity1 => _textCapacity1;

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
        FinishRecording();

        var packets = _writeIndex == 0 ? _packetBuffer0 : _packetBuffer1;
        var pCount = _writeIndex == 0 ? _packetCount0 : _packetCount1;
        var texts = _writeIndex == 0 ? _textBuffer0 : _textBuffer1;
        var tLength = _writeIndex == 0 ? _textLength0 : _textLength1;
        var circles = _writeIndex == 0 ? _circleBuffer0 : _circleBuffer1;
        var cCount = _writeIndex == 0 ? _circleCount0 : _circleCount1;

        if (pCount > 0 || cCount > 0)
        {
            NativeBindings.Engine_SubmitRenderCommands(packets, pCount, texts, tLength, circles, cCount);
        }
    }

    /// <summary>
    ///     描画スレッド向け処理としてフロントバッファに格納された確定コマンド群をC++ネイティブエンジンへ一括提出します。
    ///     論理スレッドが並行して書き込むバックバッファには一切干渉しません。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitFront()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var frontIndex = 1 - _writeIndex;
        var packets = frontIndex == 0 ? _packetBuffer0 : _packetBuffer1;
        var pCount = frontIndex == 0 ? _packetCount0 : _packetCount1;
        var texts = frontIndex == 0 ? _textBuffer0 : _textBuffer1;
        var tLength = frontIndex == 0 ? _textLength0 : _textLength1;
        var circles = frontIndex == 0 ? _circleBuffer0 : _circleBuffer1;
        var cCount = frontIndex == 0 ? _circleCount0 : _circleCount1;

        if (pCount > 0 || cCount > 0)
        {
            NativeBindings.Engine_SubmitRenderCommands(packets, pCount, texts, tLength, circles, cCount);
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
    ///     ダブルバッファの書き込み対象インデックスであり、ゼロまたは1を保持します。
    /// </summary>
    public int WriteIndex => _writeIndex;

    /// <summary>
    ///     論理スレッド向け処理として書き込み対象のバックバッファのカウンターとパイプライン状態を初期化し、新規フレームの描画コマンド記録を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginRecording()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        _packetBuffer = _writeIndex == 0 ? _packetBuffer0 : _packetBuffer1;
        _textBuffer = _writeIndex == 0 ? _textBuffer0 : _textBuffer1;
        _circleBuffer = _writeIndex == 0 ? _circleBuffer0 : _circleBuffer1;
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
    ///     論理スレッド用：現在の書き込みバッファの記録を完了し、未フラッシュの円バッチを確定させて要素数を確定・公開します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FinishRecording()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        EnsurePendingCircleBatchFlushed();
        if (_writeIndex == 0)
        {
            _packetCount0 = _currentPacketCount;
            _textLength0 = _currentTextLength;
            _circleCount0 = _currentCircleCount;
        }
        else
        {
            _packetCount1 = _currentPacketCount;
            _textLength1 = _currentTextLength;
            _circleCount1 = _currentCircleCount;
        }
    }

    /// <summary>
    ///     フロントバッファとバックバッファのスロットインデックスを交互に切り替えます。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SwapBuffers()
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        _writeIndex ^= 1;
    }

    /// <summary>
    ///     バッファスロットを反転切り替えし、次フレームの記録バッファをゼロアロケーションで準備します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap()
    {
        SwapBuffers();
        BeginRecording();
    }

    /// <summary>
    ///     描画バッファのカウンターおよびパイプラインシャドウ状態をリセットし、次フレームの記録に備えます。
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

        _packetCapacity0 = 0;
        _packetCapacity1 = 0;
        _textCapacity0 = 0;
        _textCapacity1 = 0;
        _circleCapacity0 = 0;
        _circleCapacity1 = 0;

        IsDisposed = true;
    }

    private void EnsurePacketCapacity(int extraCount)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentPacketCount + extraCount;
        var currentCapacity = _writeIndex == 0 ? _packetCapacity0 : _packetCapacity1;
        if (required <= currentCapacity) return;

        var newCapacity = Math.Max(currentCapacity * 2, required);
        var byteCount = (nuint)(sizeof(RenderPacket) * newCapacity);

        if (_writeIndex == 0)
        {
            _packetBuffer0 = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer0, byteCount, 64);
            _packetBuffer = _packetBuffer0;
            _packetCapacity0 = newCapacity;
        }
        else
        {
            _packetBuffer1 = (RenderPacket*)NativeMemory.AlignedRealloc(_packetBuffer1, byteCount, 64);
            _packetBuffer = _packetBuffer1;
            _packetCapacity1 = newCapacity;
        }
    }

    private void EnsureTextCapacity(int extraLength)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentTextLength + extraLength;
        var currentCapacity = _writeIndex == 0 ? _textCapacity0 : _textCapacity1;
        if (required <= currentCapacity) return;

        var newCapacity = Math.Max(currentCapacity * 2, required);
        var byteCount = (nuint)(sizeof(char) * newCapacity);

        if (_writeIndex == 0)
        {
            _textBuffer0 = (char*)NativeMemory.AlignedRealloc(_textBuffer0, byteCount, 64);
            _textBuffer = _textBuffer0;
            _textCapacity0 = newCapacity;
        }
        else
        {
            _textBuffer1 = (char*)NativeMemory.AlignedRealloc(_textBuffer1, byteCount, 64);
            _textBuffer = _textBuffer1;
            _textCapacity1 = newCapacity;
        }
    }

    private void EnsureCircleCapacity(int extraCount)
    {
        Debug.Assert(!IsDisposed, "[RenderCommandBuffer] 破棄済みのバッファに対して操作を実行しようとしました。");
        if (IsDisposed) return;
        var required = _currentCircleCount + extraCount;
        var currentCapacity = _writeIndex == 0 ? _circleCapacity0 : _circleCapacity1;
        if (required <= currentCapacity) return;

        var newCapacity = Math.Max(currentCapacity * 2, required);
        var byteCount = (nuint)(sizeof(CompactCirclePacket) * newCapacity);

        if (_writeIndex == 0)
        {
            _circleBuffer0 = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer0, byteCount, 64);
            _circleBuffer = _circleBuffer0;
            _circleCapacity0 = newCapacity;
        }
        else
        {
            _circleBuffer1 = (CompactCirclePacket*)NativeMemory.AlignedRealloc(_circleBuffer1, byteCount, 64);
            _circleBuffer = _circleBuffer1;
            _circleCapacity1 = newCapacity;
        }
    }
}