using System;
using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Bindings;
using AnimalBattle.Render;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ コアライブラリ (AnimalBattleCore.dll / DxLib) のウィンドウ初期化、メインループ、
/// 描画、キー入力、および終了処理を統括する静的ラッパークラス。
/// 描画色には <see cref="Color"/> を採用しています。
/// </summary>
public static class GameEngine
{
    private static readonly RenderCommandBuffer _commandBuffer = new();

    /// <summary>ジョイパッド入力: 方向キー下。</summary>
    public const int PadInputDown = 0x0001;
    /// <summary>ジョイパッド入力: 方向キー左。</summary>
    public const int PadInputLeft = 0x0002;
    /// <summary>ジョイパッド入力: 方向キー右。</summary>
    public const int PadInputRight = 0x0004;
    /// <summary>ジョイパッド入力: 方向キー上。</summary>
    public const int PadInputUp = 0x0008;
    /// <summary>ジョイパッド入力: ボタン 1。</summary>
    public const int PadInputButton1 = 0x0010;
    /// <summary>ジョイパッド入力: ボタン 2。</summary>
    public const int PadInputButton2 = 0x0020;
    /// <summary>ジョイパッド入力: ボタン 3。</summary>
    public const int PadInputButton3 = 0x0040;
    /// <summary>ジョイパッド入力: ボタン 4。</summary>
    public const int PadInputButton4 = 0x0080;
    /// <summary>ジョイパッド入力: ボタン 5。</summary>
    public const int PadInputButton5 = 0x0100;
    /// <summary>ジョイパッド入力: ボタン 6。</summary>
    public const int PadInputButton6 = 0x0200;
    /// <summary>ジョイパッド入力: ボタン 7。</summary>
    public const int PadInputButton7 = 0x0400;
    /// <summary>ジョイパッド入力: ボタン 8。</summary>
    public const int PadInputButton8 = 0x0800;

    /// <summary>
    /// ウィンドウサイズとモードを指定してネイティブエンジンを初期化します。
    /// </summary>
    /// <param name="width">画面幅 (px)</param>
    /// <param name="height">画面高さ (px)</param>
    /// <param name="windowMode">ウィンドウモードで起動するかどうか</param>
    public static void Initialize(int width, int height, bool windowMode = true)
    {
        bool initSuccess = NativeBindings.Engine_Init(IntPtr.Zero, width, height);
        Debug.Assert(initSuccess, "Native エンジンの初期化に失敗しました。");

        InputService.RegisterDefaultActions();
    }

    /// <summary>
    /// ネイティブエンジンをシャットダウンし、リソースを解放します。
    /// </summary>
    public static void Shutdown()
    {
        _commandBuffer.Dispose();
        NativeBindings.Engine_Shutdown();
    }

    /// <summary>
    /// Windows メッセージを処理します。
    /// </summary>
    /// <returns>正常継続時 true、終了要求・失敗時 false</returns>
    public static bool ProcessMessage()
    {
        return NativeBindings.Engine_ProcessMessages();
    }

    /// <summary>
    /// フレーム描画の開始処理（画面クリア等）を行います。
    /// </summary>
    /// <returns>成功時 true、失敗時 false</returns>
    public static bool BeginFrame()
    {
        _commandBuffer.Reset();
        return NativeBindings.Engine_BeginDraw();
    }

    /// <summary>
    /// フレーム描画の終了処理（コマンド一括提出・バックバッファ反転等）を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    /// <returns>成功時 true、失敗時 false</returns>
    public static bool EndFrame(float deltaTime)
    {
        _commandBuffer.Submit();
        return NativeBindings.Engine_EndDraw();
    }

    /// <summary>
    /// アプリケーション終了要求を発行します。
    /// </summary>
    public static void RequestExit()
    {
        NativeBindings.Engine_RequestExit();
    }

    /// <summary>
    /// アプリケーション終了が要求されているかを取得します。
    /// </summary>
    /// <returns>終了要求されている場合は true</returns>
    public static bool IsExitRequested()
    {
        return NativeBindings.Engine_IsExitRequested();
    }

    /// <summary>
    /// 指定されたキーコードのキーが押されているか判定します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>押されている場合は true</returns>
    public static bool IsKeyHit(int keyCode)
    {
        return NativeBindings.Input_IsKeyHit(keyCode);
    }

    /// <summary>
    /// 指定されたキーコードの押下状態を取得します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>押されている場合は true、それ以外は false</returns>
    public static bool CheckHitKey(int keyCode)
    {
        return NativeBindings.Engine_CheckHitKey(keyCode);
    }

    /// <summary>
    /// ジョイパッドの入力状態を取得します。
    /// </summary>
    /// <param name="padIndex">ジョイパッド番号 (0～3)</param>
    /// <returns>ビットマスクによる入力状態</returns>
    public static int GetJoypadInputState(int padIndex)
    {
        return NativeBindings.Engine_GetJoypadInputState(padIndex);
    }

    /// <summary>
    /// 接続されているジョイパッドの総数を取得します。
    /// </summary>
    /// <returns>接続ジョイパッド数</returns>
    public static int GetJoypadNum()
    {
        return NativeBindings.Engine_GetJoypadNum();
    }

    /// <summary>
    /// マウスカーソルの現在の画面座標を取得します。
    /// </summary>
    /// <returns>マウス座標 (<see cref="Vec2i"/>)</returns>
    public static Vec2i GetMousePoint()
    {
        Vec2i pos = Vec2i.Zero;
        unsafe
        {
            NativeBindings.Engine_GetMousePoint(&pos);
        }
        return pos;
    }

    /// <summary>
    /// マウスカーソルの現在の画面座標を取得します。
    /// </summary>
    /// <param name="x">X 座標出力先</param>
    /// <param name="y">Y 座標出力先</param>
    public static void GetMousePoint(out int x, out int y)
    {
        Vec2i pos = GetMousePoint();
        x = pos.X;
        y = pos.Y;
    }

    /// <summary>
    /// 指定画像ハンドルの幅と高さを取得します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <returns>画像サイズ (<see cref="Vec2i"/>)</returns>
    public static Vec2i GetGraphSize(int handle)
    {
        Vec2i size = Vec2i.Zero;
        unsafe
        {
            NativeBindings.Engine_GetGraphSize(handle, &size);
        }
        return size;
    }

    /// <summary>
    /// 指定画像ハンドルの幅と高さを取得します。
    /// </summary>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="width">幅出力先</param>
    /// <param name="height">高さ出力先</param>
    public static void GetGraphSize(int handle, out int width, out int height)
    {
        Vec2i size = GetGraphSize(handle);
        width = size.X;
        height = size.Y;
    }

    /// <summary>
    /// 円を画面上に描画します。
    /// </summary>
    /// <param name="center">中心座標 (<see cref="Vec2i"/>)</param>
    /// <param name="radius">半径 (px)</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawCircle(Vec2i center, int radius, Color color, bool fill)
    {
        _commandBuffer.PushDrawCircle(center.X, center.Y, radius, color.ToUint(), fill);
    }

    /// <summary>
    /// 円を画面上に描画します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    /// <param name="radius">半径 (px)</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawCircle(int x, int y, int radius, Color color, bool fill)
    {
        _commandBuffer.PushDrawCircle(x, y, radius, color.ToUint(), fill);
    }

    /// <summary>
    /// 矩形（ボックス）を画面上に描画します。
    /// </summary>
    /// <param name="p1">左上座標 (<see cref="Vec2i"/>)</param>
    /// <param name="p2">右下座標 (<see cref="Vec2i"/>)</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawBox(Vec2i p1, Vec2i p2, Color color, bool fill)
    {
        _commandBuffer.PushDrawBox(p1.X, p1.Y, p2.X, p2.Y, color.ToUint(), fill);
    }

    /// <summary>
    /// 矩形（ボックス）を画面上に描画します。
    /// </summary>
    /// <param name="x1">左上 X 座標</param>
    /// <param name="y1">左上 Y 座標</param>
    /// <param name="x2">右下 X 座標</param>
    /// <param name="y2">右下 Y 座標</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawBox(int x1, int y1, int x2, int y2, Color color, bool fill)
    {
        _commandBuffer.PushDrawBox(x1, y1, x2, y2, color.ToUint(), fill);
    }

    /// <summary>
    /// 線分を画面上に描画します。
    /// </summary>
    /// <param name="p1">始点座標 (<see cref="Vec2i"/>)</param>
    /// <param name="p2">終点座標 (<see cref="Vec2i"/>)</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="thickness">線の太さ</param>
    public static void DrawLine(Vec2i p1, Vec2i p2, Color color, int thickness = 1)
    {
        _commandBuffer.PushDrawLine(p1.X, p1.Y, p2.X, p2.Y, color.ToUint(), thickness);
    }

    /// <summary>
    /// 線分を画面上に描画します。
    /// </summary>
    /// <param name="x1">始点 X 座標</param>
    /// <param name="y1">始点 Y 座標</param>
    /// <param name="x2">終点 X 座標</param>
    /// <param name="y2">終点 Y 座標</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    /// <param name="thickness">線の太さ</param>
    public static void DrawLine(int x1, int y1, int x2, int y2, Color color, int thickness = 1)
    {
        _commandBuffer.PushDrawLine(x1, y1, x2, y2, color.ToUint(), thickness);
    }

    /// <summary>
    /// 指定座標に文字列を描画します。
    /// </summary>
    /// <param name="pos">描画座標 (<see cref="Vec2i"/>)</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    public static void DrawString(Vec2i pos, string text, Color color)
    {
        _commandBuffer.PushDrawString(pos.X, pos.Y, text, color.ToUint());
    }

    /// <summary>
    /// 指定座標に文字列を描画します。
    /// </summary>
    /// <param name="x">X 座標</param>
    /// <param name="y">Y 座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    public static void DrawString(int x, int y, string text, Color color)
    {
        _commandBuffer.PushDrawString(x, y, text, color.ToUint());
    }

    /// <summary>
    /// 指定座標に文字列を描画します。
    /// </summary>
    /// <param name="x">X 座標</param>
    /// <param name="y">Y 座標</param>
    /// <param name="text">描画文字列 (<see cref="ReadOnlySpan{Char}"/>)</param>
    /// <param name="color">描画カラー (<see cref="Color"/>)</param>
    public static void DrawString(int x, int y, ReadOnlySpan<char> text, Color color)
    {
        _commandBuffer.PushDrawString(x, y, text, color.ToUint());
    }

    /// <summary>
    /// 指定画像ハンドルを画面上に描画します。
    /// </summary>
    /// <param name="pos">描画先座標 (<see cref="Vec2i"/>)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過を有効にするかどうか</param>
    public static void DrawGraph(Vec2i pos, int handle, bool trans)
    {
        _commandBuffer.PushDrawGraph(pos.X, pos.Y, handle, trans);
    }

    /// <summary>
    /// 指定画像ハンドルを画面上に描画します。
    /// </summary>
    /// <param name="x">描画先 X 座標</param>
    /// <param name="y">描画先 Y 座標</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過を有効にするかどうか</param>
    public static void DrawGraph(int x, int y, int handle, bool trans)
    {
        _commandBuffer.PushDrawGraph(x, y, handle, trans);
    }

    /// <summary>
    /// 画像ハンドルを中心に回転・拡大縮小して描画します。
    /// </summary>
    /// <param name="center">描画中心座標 (<see cref="Vec2i"/>)</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過描画を有効にするかどうか</param>
    public static void DrawRotaGraph(Vec2i center, double scale, double angle, int handle, bool trans)
    {
        _commandBuffer.PushDrawRotaGraph(center.X, center.Y, (float)scale, (float)angle, handle, trans);
    }

    /// <summary>
    /// スプライトシートから指定矩形を切り出し、回転・拡縮・左右反転を行って描画します。
    /// </summary>
    /// <param name="center">描画中心座標 (<see cref="Vec2i"/>)</param>
    /// <param name="srcPos">切り出し開始座標 (<see cref="Vec2i"/>)</param>
    /// <param name="srcSize">切り出しサイズ (<see cref="Vec2i"/>)</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過描画を有効にするかどうか</param>
    /// <param name="turnX">左右反転描画を行うかどうか</param>
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
        _commandBuffer.PushDrawRectRotaGraph(
            center.X, center.Y,
            srcPos.X, srcPos.Y,
            srcSize.X, srcSize.Y,
            (float)scale, (float)angle,
            handle,
            trans,
            turnX);
    }

    /// <summary>
    /// スプライトシートから指定矩形を切り出し、回転・拡縮・左右反転を行って描画します。
    /// </summary>
    /// <param name="x">描画中心 X 座標</param>
    /// <param name="y">描画中心 Y 座標</param>
    /// <param name="srcX">切り出し開始 X 座標</param>
    /// <param name="srcY">切り出し開始 Y 座標</param>
    /// <param name="width">切り出し幅</param>
    /// <param name="height">切り出し高さ</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="trans">透過描画を有効にするかどうか</param>
    /// <param name="turnX">左右反転描画を行うかどうか</param>
    public static void DrawRectRotaGraph(
        int x,
        int y,
        int srcX,
        int srcY,
        int width,
        int height,
        double scale,
        double angle,
        int handle,
        bool trans,
        bool turnX)
    {
        _commandBuffer.PushDrawRectRotaGraph(
            x, y,
            srcX, srcY,
            width, height,
            (float)scale, (float)angle,
            handle,
            trans,
            turnX);
    }

    /// <summary>
    /// 描画ブレンドモードおよびパラメータを設定します。
    /// </summary>
    /// <param name="mode">ブレンドモード (<see cref="BlendMode"/>)</param>
    /// <param name="pal">ブレンドパラメータ (アルファ値 / 輝度など: 0～255)</param>
    public static void SetDrawBlendMode(BlendMode mode, int pal = 255)
    {
        _commandBuffer.PushSetDrawBlendMode((int)mode, pal);
    }

    /// <summary>
    /// 描画輝度（RGB 明るさ）を設定します。
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
/// 描画合成ブレンドモード。
/// </summary>
public enum BlendMode
{
    /// <summary>ノーブレンド（通常描画）</summary>
    NoBlend = 0,

    /// <summary>アルファブレンド（透過合成）</summary>
    Alpha = 1,

    /// <summary>加算ブレンド（発光・閃白合成）</summary>
    Add = 2,

    /// <summary>減算ブレンド</summary>
    Sub = 3,

    /// <summary>乗算ブレンド</summary>
    Mul = 4,
}

