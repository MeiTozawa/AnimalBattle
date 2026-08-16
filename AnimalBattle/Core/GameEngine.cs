using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// Native C++ コアライブラリ (AnimalBattleCore.dll / DxLib) のウィンドウ初期化、メインループ、
/// 描画、キー入力、および終了処理を統括する静的ラッパークラス。
/// </summary>
public static class GameEngine
{
    public const int PadInputDown = 0x0001;
    public const int PadInputLeft = 0x0002;
    public const int PadInputRight = 0x0004;
    public const int PadInputUp = 0x0008;
    public const int PadInputButton1 = 0x0010;
    public const int PadInputButton2 = 0x0020;
    public const int PadInputButton3 = 0x0040;
    public const int PadInputButton4 = 0x0080;
    public const int PadInputButton5 = 0x0100;
    public const int PadInputButton6 = 0x0200;
    public const int PadInputButton7 = 0x0400;
    public const int PadInputButton8 = 0x0800;

    /// <summary>
    /// ウィンドウサイズとモードを指定してネイティブエンジンを初期化します。
    /// </summary>
    /// <param name="width">画面幅 (px)</param>
    /// <param name="height">画面高さ (px)</param>
    /// <param name="windowMode">ウィンドウモードで起動するかどうか</param>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int Initialize(int width, int height, bool windowMode = true)
    {
        return NativeBindings.Engine_Init(IntPtr.Zero, width, height) ? 0 : -1;
    }

    /// <summary>
    /// ネイティブエンジンをシャットダウンし、リソースを解放します。
    /// </summary>
    public static void Shutdown()
    {
        NativeBindings.Engine_Shutdown();
    }

    /// <summary>
    /// Windows メッセージを処理します。
    /// </summary>
    /// <returns>正常時 0、終了要求時 -1</returns>
    public static int ProcessMessage()
    {
        return NativeBindings.Engine_ProcessMessages() ? 0 : -1;
    }

    /// <summary>
    /// フレーム描画の開始処理（画面クリア等）を行います。
    /// </summary>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int BeginFrame()
    {
        return NativeBindings.Engine_BeginDraw() ? 0 : -1;
    }

    /// <summary>
    /// フレーム描画の終了処理（バックバッファ反転等）を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int EndFrame(float deltaTime)
    {
        return NativeBindings.Engine_EndDraw() ? 0 : -1;
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
    /// <returns>押されている場合は 1、それ以外は 0</returns>
    public static int CheckHitKey(int keyCode)
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
    /// 円を画面上に描画します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    /// <param name="radius">半径 (px)</param>
    /// <param name="color">描画カラー (ARGB)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawCircle(int x, int y, int radius, uint color, bool fill)
    {
        NativeBindings.Draw_Circle(x, y, radius, color, fill);
    }

    /// <summary>
    /// 矩形（ボックス）を画面上に描画します。
    /// </summary>
    /// <param name="x1">左上 X 座標</param>
    /// <param name="y1">左上 Y 座標</param>
    /// <param name="x2">右下 X 座標</param>
    /// <param name="y2">右下 Y 座標</param>
    /// <param name="color">描画カラー (ARGB)</param>
    /// <param name="fill">塗りつぶすかどうか</param>
    public static void DrawBox(int x1, int y1, int x2, int y2, uint color, bool fill)
    {
        NativeBindings.Draw_Box(x1, y1, x2, y2, color, fill);
    }

    /// <summary>
    /// 指定座標に文字列を描画します。
    /// </summary>
    /// <param name="x">X 座標</param>
    /// <param name="y">Y 座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー (ARGB)</param>
    public static void DrawString(int x, int y, string text, uint color)
    {
        NativeBindings.Draw_String(x, y, text, color);
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
        NativeBindings.Draw_Graph(x, y, handle, trans);
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
        NativeBindings.Draw_RectRotaGraph(x, y, srcX, srcY, width, height, scale, angle, handle, trans, turnX);
    }
}
