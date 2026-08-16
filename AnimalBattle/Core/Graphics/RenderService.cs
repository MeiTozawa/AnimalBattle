using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// Native C++ 描画サービス (DxLib バックエンド) との高速インターフェースを提供するサービスラッパークラス。
/// </summary>
public class RenderService
{
    private readonly IntPtr _nativePtr;

    /// <summary>
    /// Native 側の RenderService インスタンスポインタを指定して初期化します。
    /// </summary>
    /// <param name="nativePtr">Native C++ ポインタ</param>
    public RenderService(IntPtr nativePtr)
    {
        _nativePtr = nativePtr;
    }

    /// <summary>
    /// カメラ座標系への追従変換を有効または無効にします。
    /// </summary>
    /// <param name="enabled">カメラ追従を有効にするかどうか</param>
    public void SetCameraEnabled(bool enabled)
    {
        NativeBindings.Render_SetCameraEnabled(_nativePtr, enabled);
    }

    /// <summary>
    /// カメラの中心座標および拡大率を更新します。
    /// </summary>
    /// <param name="x">カメラ中心 X 座標</param>
    /// <param name="y">カメラ中心 Y 座標</param>
    /// <param name="scale">拡大率</param>
    public void SetCameraState(float x, float y, float scale)
    {
        NativeBindings.Render_SetCameraState(_nativePtr, x, y, scale);
    }

    /// <summary>
    /// 矩形（ボックス）を描画します。
    /// </summary>
    /// <param name="rect">描画矩形</param>
    /// <param name="color">描画カラー (ARGB)</param>
    /// <param name="fillFlag">塗りつぶすかどうか</param>
    public void DrawBox(Rect<int> rect, uint color, bool fillFlag)
    {
        NativeBindings.Render_DrawBox(_nativePtr, rect, color, fillFlag);
    }

    /// <summary>
    /// 指定座標に文字列を描画します。
    /// </summary>
    /// <param name="point">描画座標</param>
    /// <param name="text">描画文字列</param>
    /// <param name="color">描画カラー (ARGB)</param>
    public void DrawString(Point<int> point, string text, uint color)
    {
        NativeBindings.Render_DrawString(_nativePtr, point, text, color);
    }

    /// <summary>
    /// 外枠ボーダー付きで文字列を描画します。
    /// </summary>
    /// <param name="rect">描画領域矩形</param>
    /// <param name="text">描画文字列</param>
    /// <param name="shape">外枠形状 (Rectangle / RoundRect)</param>
    /// <param name="textColor">文字カラー (ARGB)</param>
    /// <param name="borderColor">ボーダーカラー (ARGB)</param>
    public void DrawStringWithBorder(Rect<int> rect, string text, BorderShape shape, uint textColor, uint borderColor)
    {
        NativeBindings.Render_DrawStringWithBorder(_nativePtr, rect, text, shape, textColor, borderColor);
    }

    /// <summary>
    /// 画像グラフィックを画面上に描画します。
    /// </summary>
    /// <param name="point">描画座標</param>
    /// <param name="handle">画像ハンドル</param>
    /// <param name="transFlag">透過を有効にするかどうか</param>
    public void DrawGraph(Point<int> point, int handle, bool transFlag)
    {
        NativeBindings.Render_DrawGraph(_nativePtr, point, handle, transFlag);
    }

    /// <summary>
    /// スプライトシートから指定矩形を切り出し、回転・拡縮・左右反転を行って描画します。
    /// </summary>
    /// <param name="point">描画中心座標</param>
    /// <param name="src">切り出し領域</param>
    /// <param name="extRate">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    /// <param name="graphHandle">画像ハンドル</param>
    /// <param name="transFlag">透過を有効にするかどうか</param>
    /// <param name="turnFlag">左右反転描画を行うかどうか</param>
    public void DrawRectRotaGraph(
        Point<int> point,
        Rect<int> src,
        double extRate,
        double angle,
        int graphHandle,
        bool transFlag,
        bool turnFlag)
    {
        NativeBindings.Render_DrawRectRotaGraph(_nativePtr, point, src, extRate, angle, graphHandle, transFlag, turnFlag);
    }
}
