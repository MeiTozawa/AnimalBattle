using System.Drawing;
using System.Runtime.CompilerServices;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;

/// <summary>
///     Native C++ コアライブラリ (AnimalBattleCore.dll / DxLib) のウィンドウ初期化、メインループ、
///     描画、キー入力、および終了処理を統括する静的ラッパークラス。
///     描画色には <see cref="Color" /> を採用しています。
/// </summary>
public static class GameEngine {
    private static readonly RenderCommandBuffer _commandBuffer = new();

    /// <summary>
    ///     ウィンドウサイズとモードを指定してネイティブエンジンを初期化します。
    /// </summary>
    /// <param name="width">画面幅 (px)</param>
    /// <param name="height">画面高さ (px)</param>
    /// <param name="windowMode">ウィンドウモードで起動するかどうか</param>
    public static void Initialize(int width, int height, bool windowMode = true)
    {
        var initSuccess = NativeBindings.Engine_Init(IntPtr.Zero, width, height);
        Debug.Assert(initSuccess, "Native エンジンの初期化に失敗しました。");

        InputService.RegisterDefaultActions();
        SoundService.Initialize();
    }

    /// <summary>
    ///     ネイティブエンジンをシャットダウンし、リソースを解放します。
    /// </summary>
    public static void Shutdown()
    {
        SoundService.Shutdown();
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
    ///     論理スレッド用：新規フレームの描画コマンド記録を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginRecording()
    {
        NativeBindings.Input_Snapshot();
        _commandBuffer.BeginRecording();
    }

    /// <summary>
    ///     論理スレッド用：描画コマンド記録を完了し、前台バッファへ公開します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FinishRecording()
    {
        _commandBuffer.FinishRecording();
    }

    /// <summary>
    ///     レンダリングスレッド用：前台バッファ（Front Buffer）に蓄積された描画コマンドを C++ へ一括提出します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubmitFront()
    {
        _commandBuffer.SubmitFront();
    }

    /// <summary>
    ///     前・後台バッファのスロットを切り替えます。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SwapBuffers()
    {
        _commandBuffer.SwapBuffers();
    }

    /// <summary>
    ///     レンダリングスレッド用：画面クリアおよび描画準備を開始します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BeginDraw()
    {
        return NativeBindings.Engine_BeginDraw();
    }

    /// <summary>
    ///     レンダリングスレッド用：バックバッファの画面反転（ScreenFlip / VSync）を実行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndDraw()
    {
        return NativeBindings.Engine_EndDraw();
    }



    /// <summary>
    ///     アプリケーション終了要求を発行します。
    /// </summary>
    public static void RequestExit()
    {
        NativeBindings.Engine_RequestExit();
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
    ///     ジョイパッドの入力状態を取得します。
    /// </summary>
    /// <param name="padIndex">ジョイパッド番号 (0～3)</param>
    /// <returns>ビットマスクによる入力状態</returns>
    public static int GetJoypadInputState(int padIndex)
    {
        return NativeBindings.Engine_GetJoypadInputState(padIndex);
    }

    /// <summary>
    ///     接続されているジョイパッドの総数を取得します。
    /// </summary>
    /// <returns>接続ジョイパッド数</returns>
    public static int GetJoypadNum()
    {
        return NativeBindings.Engine_GetJoypadNum();
    }

    /// <summary>
    ///     マウスカーソルの現在の画面座標を取得します。
    /// </summary>
    /// <returns>マウス座標 (<see cref="Vec2i" />)</returns>
    public static Vec2i GetMousePoint()
    {
        var pos = Vec2i.Zero;
        unsafe
        {
            NativeBindings.Engine_GetMousePoint(&pos);
        }

        return pos;
    }

    /// <summary>
    ///     指定画像ハンドルの幅と高さを取得します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>画像サイズ (<see cref="Vec2i" />)</returns>
    public static Vec2i GetGraphSize(int handle)
    {
        var size = Vec2i.Zero;
        unsafe
        {
            NativeBindings.Engine_GetGraphSize(handle, &size);
        }

        return size;
    }

    /// <summary>
    ///     カメラシェイク等によるグローバル描画オフセット（ピクセル）。
    /// </summary>
    public static Vec2i CameraOffset { get; private set; }

    /// <summary>
    ///     グローバルカメラ描画オフセットを設定します。
    /// </summary>
    /// <param name="offset">オフセット (<see cref="Vec2i" />)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetCameraOffset(Vec2i offset)
    {
        CameraOffset = offset;
    }

    /// <summary>
    ///     円を画面上に描画します。
    /// </summary>
    /// <param name="center">中心座標 (<see cref="Vec2i" />)</param>
    /// <param name="radius">半径 (px)</param>
    /// <param name="color">描画カラー (<see cref="Color" />)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawCircle(Vec2i center, int radius, Color color, bool fill)
    {
        _commandBuffer.PushDrawCircle(center + CameraOffset, radius, color.ToUint(), fill);
    }

    /// <summary>
    ///     高密度円描画コマンド群をバッチメモリコピーで一括追加します（20 バイト緊密配置ストリーム）。
    /// </summary>
    /// <param name="circles">緊密配置された円パケットの読み取り専用スパン。</param>
    public static void DrawCirclesBatch(ReadOnlySpan<CompactCirclePacket> circles)
    {
        if (CameraOffset == Vec2i.Zero)
        {
            _commandBuffer.PushCompactCirclesBatch(circles);
        }
        else
        {
            for (var i = 0; i < circles.Length; i++)
            {
                var c = circles[i];
                _commandBuffer.PushDrawCircle(c.Center + CameraOffset, c.Radius, c.Color, c.Fill != 0);
            }
        }
    }

    /// <summary>
    ///     矩形（ボックス）を画面上に描画します。
    /// </summary>
    /// <param name="p1">左上座標 (<see cref="Vec2i" />)</param>
    /// <param name="p2">右下座標 (<see cref="Vec2i" />)</param>
    /// <param name="color">描画カラー (<see cref="Color" />)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawBox(Vec2i p1, Vec2i p2, Color color, bool fill)
    {
        _commandBuffer.PushDrawBox(p1 + CameraOffset, p2 + CameraOffset, color.ToUint(), fill);
    }

    /// <summary>
    ///     線分を画面上に描画します。
    /// </summary>
    /// <param name="p1">始点座標 (<see cref="Vec2i" />)</param>
    /// <param name="p2">終点座標 (<see cref="Vec2i" />)</param>
    /// <param name="color">描画カラー (<see cref="Color" />)</param>
    /// <param name="thickness">線の太さ</param>
    public static void DrawLine(Vec2i p1, Vec2i p2, Color color, int thickness = 1)
    {
        _commandBuffer.PushDrawLine(p1 + CameraOffset, p2 + CameraOffset, color.ToUint(), thickness);
    }

    /// <summary>
    ///     指定座標に文字列を描画します。
    /// </summary>
    /// <param name="pos">描画座標 (<see cref="Vec2i" />)</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー (<see cref="Color" />)</param>
    public static void DrawString(Vec2i pos, string text, Color color)
    {
        _commandBuffer.PushDrawString(pos + CameraOffset, text, color.ToUint());
    }

    /// <summary>
    ///     指定座標に文字列を描画します。
    /// </summary>
    /// <param name="pos">描画座標 (<see cref="Vec2i" />)</param>
    /// <param name="text">描画文字列 (<see cref="ReadOnlySpan{Char}" />)</param>
    /// <param name="color">描画カラー (<see cref="Color" />)</param>
    public static void DrawString(Vec2i pos, ReadOnlySpan<char> text, Color color)
    {
        _commandBuffer.PushDrawString(pos + CameraOffset, text, color.ToUint());
    }

    /// <summary>
    ///     指定画像ハンドルを画面上に描画します。
    /// </summary>
    /// <param name="pos">描画先座標 (<see cref="Vec2i" />)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過を有効にするかどうか</param>
    public static void DrawGraph(Vec2i pos, int handle, bool trans)
    {
        _commandBuffer.PushDrawGraph(pos + CameraOffset, handle, trans);
    }

    /// <summary>
    ///     画像ハンドルを中心に回転・拡大縮小して描画します。
    /// </summary>
    /// <param name="center">描画中心座標 (<see cref="Vec2i" />)</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過描画を有効にするかどうか</param>
    public static void DrawRotaGraph(Vec2i center, double scale, double angle, int handle, bool trans)
    {
        _commandBuffer.PushDrawRotaGraph(center + CameraOffset, (float)scale, (float)angle, handle, trans);
    }

    /// <summary>
    ///     スプライトシートから指定矩形を切り出し、回転・独立 X/Y 軸拡縮・左右反転を行って描画します。
    /// </summary>
    /// <param name="center">描画中心座標 (<see cref="Vec2i" />)</param>
    /// <param name="srcPos">切り出し開始座標 (<see cref="Vec2i" />)</param>
    /// <param name="srcSize">切り出しサイズ (<see cref="Vec2i" />)</param>
    /// <param name="scale">拡大率 (<see cref="Vec2f" />)</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
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
    ///     スプライトシートから指定矩形を切り出し、回転・独立 X/Y 軸拡縮・左右反転を行って描画します (DxLib DrawRectRotaGraph3 相当)。
    /// </summary>
    public static void DrawRectRotaGraph3(
        Vec2i center,
        Vec2i srcPos,
        Vec2i srcSize,
        Vec2f scale,
        double angle,
        int handle,
        bool trans,
        bool turnX = false)
    {
        DrawRectRotaGraph(center, srcPos, srcSize, scale, angle, handle, trans, turnX);
    }

    /// <summary>
    ///     画像ハンドルを中心に独立 X/Y 軸拡縮・回転して描画します (DxLib DrawRotaGraph3 相当)。
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
    /// <param name="mode">ブレンドモード (<see cref="BlendMode" />)</param>
    /// <param name="pal">ブレンドパラメータ (アルファ値 / 輝度など: 0～255)</param>
    public static void SetDrawBlendMode(BlendMode mode, int pal = 255)
    {
        _commandBuffer.PushSetDrawBlendMode((int)mode, pal);
    }

    /// <summary>
    ///     描画輝度（RGB 明るさ）を設定します。
    /// </summary>
    /// <param name="r">赤色輝度 (0～255)</param>
    /// <param name="g">緑色輝度 (0～255)</param>
    /// <param name="b">青色輝度 (0～255)</param>
    public static void SetDrawBright(int r, int g, int b)
    {
        _commandBuffer.PushSetDrawBright(r, g, b);
    }
}

/// <summary>
///     描画合成ブレンドモード。
/// </summary>
public enum BlendMode {
    /// <summary>ノーブレンド（通常描画）</summary>
    NoBlend = 0,

    /// <summary>アルファブレンド（透過合成）</summary>
    Alpha = 1,

    /// <summary>加算ブレンド（発光・閃白合成）</summary>
    Add = 2,

    /// <summary>減算ブレンド</summary>
    Sub = 3,

    /// <summary>乗算ブレンド</summary>
    Mul = 4
}