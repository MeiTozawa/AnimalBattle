using AnimalBattle.Logging;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AnimalBattle.Core;

/// <summary>
///     Native C++ コアライブラリ AnimalBattleCoreおよびDxLib のウィンドウ初期化、メインループ、
///     描画、キー入力、および終了処理を統括する静的ラッパークラス。
///     描画色には <see cref="Color" /> を採用しています。
/// </summary>
public static class GameEngine {
    private static readonly RenderCommandBuffer _commandBuffer = new();

    /// <summary>
    ///     ウィンドウサイズを指定してネイティブエンジンを初期化します。
    /// </summary>
    /// <param name="width">画面幅 ピクセル単位</param>
    /// <param name="height">画面高さ ピクセル単位</param>
    public static void Initialize(int width, int height)
    {
        var initSuccess = NativeBindings.Engine_Init(IntPtr.Zero, width, height);
        Debug.Assert(initSuccess, "Native エンジンの初期化に失敗しました。");
        if (!initSuccess) Log.Error("GameEngine", "ネイティブエンジンの初期化に失敗しました。");

        InputService.RegisterDefaultActions();
        SoundService.Initialize();
        Log.Info("GameEngine", $"エンジンの初期化が完了しました ({width}x{height})。");
    }

    /// <summary>
    ///     ネイティブエンジンをシャットダウンし、リソースを解放します。
    /// </summary>
    public static void Shutdown()
    {
        Log.Info("GameEngine", "エンジンをシャットダウンします。");
        SoundService.Shutdown();
        EventBus.ClearAll();
        _commandBuffer.Dispose();
        NativeBindings.Engine_Shutdown();
    }

    /// <summary>
    ///     Windows メッセージを処理します。
    /// </summary>
    /// <returns>正常継続時 true、終了要求・失敗時 false</returns>
    public static bool ProcessMessage()
    {
        return NativeBindings.Engine_ProcessMessages();
    }

    /// <summary>
    ///     入力スナップショットを確定し、新規フレームの描画コマンド記録を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginRecording()
    {
        NativeBindings.Input_Snapshot();
        _commandBuffer.BeginRecording();
    }

    /// <summary>
    ///     現在記録された描画コマンドをネイティブエンジンへ一括提出します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Submit()
    {
        _commandBuffer.Submit();
    }

    /// <summary>
    ///     画面クリアおよび描画準備を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BeginDraw()
    {
        return NativeBindings.Engine_BeginDraw();
    }

    /// <summary>
    ///     バックバッファの画面反転（ScreenFlip / VSync）を実行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndDraw()
    {
        return NativeBindings.Engine_EndDraw();
    }

    /// <summary>
    ///     アプリケーション終了が要求されているかを取得します。
    /// </summary>
    /// <returns>終了要求されている場合は true</returns>
    public static bool IsExitRequested()
    {
        return NativeBindings.Engine_IsExitRequested();
    }

    /// <summary>
    ///     指定されたキーコードの押下状態を取得します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>押されている場合は true、それ以外は false</returns>
    public static bool CheckHitKey(int keyCode)
    {
        return NativeBindings.Engine_CheckHitKey(keyCode);
    }

    /// <summary>
    ///     指定画像ハンドルの幅と高さを取得します。
    ///     双スレッドレンダリング競態防止のため、AssetService のキャッシュから参照します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>画像サイズ <see cref="Vec2i" /></returns>
    public static Vec2i GetGraphSize(int handle)
    {
        return AssetService.GetGraphSize(handle);
    }

    /// <summary>
    ///     カメラシェイク等によるグローバル描画オフセットピクセル単位。
    /// </summary>
    public static Vec2i CameraOffset { get; private set; }

    /// <summary>
    ///     グローバルカメラ描画オフセットを設定します。
    /// </summary>
    /// <param name="offset">オフセット <see cref="Vec2i" /></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetCameraOffset(Vec2i offset)
    {
        CameraOffset = offset;
    }

    /// <summary>
    ///     円を画面上に描画します。
    /// </summary>
    /// <param name="center">中心座標 <see cref="Vec2i" /></param>
    /// <param name="radius">半径 ピクセル単位</param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawCircle(Vec2i center, int radius, Color color, bool fill)
    {
        _commandBuffer.PushDrawCircle(center + CameraOffset, radius, color.ToUint(), fill);
    }

    /// <summary>
    ///     高密度円描画コマンド群を一括追加します：20 バイト緊密配置ストリーム。
    ///     カメラオフセットを考慮した高速流し込みパイプラインへ直接委託されます。
    /// </summary>
    /// <param name="circles">緊密配置された円パケットの読み取り専用スパン。</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawCirclesBatch(ReadOnlySpan<CompactCirclePacket> circles)
    {
        _commandBuffer.PushCompactCirclesBatch(circles, CameraOffset);
    }

    /// <summary>
    ///     矩形：ボックスを画面上に描画します。
    /// </summary>
    /// <param name="p1">左上座標 <see cref="Vec2i" /></param>
    /// <param name="p2">右下座標 <see cref="Vec2i" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawBox(Vec2i p1, Vec2i p2, Color color, bool fill)
    {
        _commandBuffer.PushDrawBox(p1 + CameraOffset, p2 + CameraOffset, color.ToUint(), fill);
    }

    /// <summary>
    ///     線分を画面上に描画します。
    /// </summary>
    /// <param name="p1">始点座標 <see cref="Vec2i" /></param>
    /// <param name="p2">終点座標 <see cref="Vec2i" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    /// <param name="thickness">線の太さ</param>
    public static void DrawLine(Vec2i p1, Vec2i p2, Color color, int thickness = 1)
    {
        _commandBuffer.PushDrawLine(p1 + CameraOffset, p2 + CameraOffset, color.ToUint(), thickness);
    }

    /// <summary>
    ///     指定座標と配置基準に従って文字列を描画します。
    /// </summary>
    /// <param name="pos">描画基準座標 <see cref="Vec2i" /></param>
    /// <param name="text">描画文字列 <see cref="ReadOnlySpan{Char}" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    /// <param name="alignment">配置基準。省略時は左上基準</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawString(
        Vec2i pos,
        ReadOnlySpan<char> text,
        Color color,
        TextAlignment alignment = TextAlignment.TopLeft)
    {
        _commandBuffer.PushDrawString(pos + CameraOffset, text, color.ToUint(), alignment);
    }

    /// <summary>
    ///     水平中央・垂直中央揃えで文字列を描画します。
    /// </summary>
    /// <param name="pos">中心座標 <see cref="Vec2i" /></param>
    /// <param name="text">描画文字列 <see cref="ReadOnlySpan{Char}" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawCenterString(Vec2i pos, ReadOnlySpan<char> text, Color color)
    {
        DrawString(pos, text, color, TextAlignment.Center);
    }

    /// <summary>
    ///     左揃え・垂直中央揃えで文字列を描画します。
    /// </summary>
    /// <param name="pos">左端中央座標 <see cref="Vec2i" /></param>
    /// <param name="text">描画文字列 <see cref="ReadOnlySpan{Char}" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLeftString(Vec2i pos, ReadOnlySpan<char> text, Color color)
    {
        DrawString(pos, text, color, TextAlignment.Left);
    }

    /// <summary>
    ///     右揃え・垂直中央揃えで文字列を描画します。
    /// </summary>
    /// <param name="pos">右端中央座標 <see cref="Vec2i" /></param>
    /// <param name="text">描画文字列 <see cref="ReadOnlySpan{Char}" /></param>
    /// <param name="color">描画カラー <see cref="Color" /></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRightString(Vec2i pos, ReadOnlySpan<char> text, Color color)
    {
        DrawString(pos, text, color, TextAlignment.Right);
    }

    /// <summary>
    ///     スプライトシートから指定矩形を切り出し、回転・独立 X/Y 軸拡縮・左右反転を行って描画します。
    /// </summary>
    /// <param name="center">描画中心座標 <see cref="Vec2i" /></param>
    /// <param name="srcPos">切り出し開始座標 <see cref="Vec2i" /></param>
    /// <param name="srcSize">切り出しサイズ <see cref="Vec2i" /></param>
    /// <param name="scale">拡大率 <see cref="Vec2f" /></param>
    /// <param name="angle">回転角度 ラジアン単位</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過描画を有効にするかどうか</param>
    /// <param name="turnX">左右反転描画を行うかどうか</param>
    public static void DrawRectRotaGraph(
        Vec2i center,
        Vec2i srcPos,
        Vec2i srcSize,
        Vec2f scale,
        double angle,
        int handle,
        bool trans,
        bool turnX)
    {
        _commandBuffer.PushDrawRectRotaGraph(
            center + CameraOffset,
            srcPos,
            srcSize,
            scale,
            (float)angle,
            handle,
            trans,
            turnX);
    }

    /// <summary>
    ///     スプライトシートから指定矩形を切り出し、回転・等比拡縮・左右反転を行って描画します。
    /// </summary>
    public static void DrawRectRotaGraph(
        Vec2i center,
        Vec2i srcPos,
        Vec2i srcSize,
        double scale,
        double angle,
        int handle,
        bool trans,
        bool turnX)
    {
        DrawRectRotaGraph(center, srcPos, srcSize, new Vec2f((float)scale, (float)scale), angle, handle, trans, turnX);
    }

    /// <summary>
    ///     画像ハンドルを中心に独立 X/Y 軸拡縮・回転して描画します DxLibのDrawRotaGraph3相当。
    /// </summary>
    public static void DrawRotaGraph3(
        Vec2i center,
        Vec2f scale,
        double angle,
        int handle,
        bool trans,
        bool turnX = false)
    {
        var size = GetGraphSize(handle);
        DrawRectRotaGraph(center, Vec2i.Zero, size, scale, angle, handle, trans, turnX);
    }

    /// <summary>
    ///     描画ブレンドモードおよびパラメータを設定します。
    /// </summary>
    /// <param name="mode">ブレンドモード <see cref="BlendMode" /></param>
    /// <param name="pal">ブレンドパラメータ 0から255の範囲のアルファ値または輝度</param>
    public static void SetDrawBlendMode(BlendMode mode, int pal = 255)
    {
        _commandBuffer.PushSetDrawBlendMode((int)mode, pal);
    }

    /// <summary>
    ///     描画輝度：RGB 明るさを設定します。
    /// </summary>
    /// <param name="r">赤色輝度 0から255の範囲</param>
    /// <param name="g">緑色輝度 0から255の範囲</param>
    /// <param name="b">青色輝度 0から255の範囲</param>
    public static void SetDrawBright(int r, int g, int b)
    {
        _commandBuffer.PushSetDrawBright(r, g, b);
    }
}

/// <summary>
///     描画合成ブレンドモード。
/// </summary>
public enum BlendMode {
    /// <summary>ノーブレンド：通常描画</summary>
    NoBlend = 0,

    /// <summary>アルファブレンド：透過合成</summary>
    Alpha = 1,

    /// <summary>加算ブレンド：発光・閃白合成</summary>
    Add = 2
}