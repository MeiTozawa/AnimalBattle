using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// Native C++ <see cref="AnimationPlayer"/> インスタンスを生成します。
    /// </summary>
    /// <returns>生成されたインスタンスのネイティブポインタ（失敗時は <see cref="IntPtr.Zero"/>）</returns>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Create")]
    public static partial IntPtr AnimationPlayerCreate();

    /// <summary>
    /// Native C++ <see cref="AnimationPlayer"/> インスタンスを破棄します。
    /// </summary>
    /// <param name="player">破棄するインスタンスのネイティブポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Destroy")]
    public static partial void AnimationPlayerDestroy(IntPtr player);

    /// <summary>
    /// 格子状のスプライトシートから指定されたアニメーションクリップを登録します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <param name="name">アニメーション識別名</param>
    /// <param name="row">スプライトシートの行インデックス (0 開始)</param>
    /// <param name="frameWidth">1 コマの幅 (ピクセル)</param>
    /// <param name="frameHeight">1 コマの高さ (ピクセル)</param>
    /// <param name="frameCount">コマ数</param>
    /// <param name="frameDuration">1 コマの表示時間 (秒)</param>
    /// <param name="isLooping">ループ再生フラグ</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_AddGridClip", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void AnimationPlayerAddGridClip(
        IntPtr player,
        string name,
        int row,
        int frameWidth,
        int frameHeight,
        int frameCount,
        float frameDuration,
        [MarshalAs(UnmanagedType.U1)] bool isLooping);

    /// <summary>
    /// 指定された名前のアニメーション再生を開始します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <param name="name">再生するアニメーション名</param>
    /// <param name="restartIfSame">同一アニメーション再生中でも最初からやり直すか</param>
    /// <returns>再生成功時は <c>true</c></returns>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Play", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AnimationPlayerPlay(IntPtr player, string name, [MarshalAs(UnmanagedType.U1)] bool restartIfSame);

    /// <summary>
    /// 再生を一時停止します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Pause")]
    [SuppressGCTransition]
    public static partial void AnimationPlayerPause(IntPtr player);

    /// <summary>
    /// 再生を再開します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Resume")]
    [SuppressGCTransition]
    public static partial void AnimationPlayerResume(IntPtr player);

    /// <summary>
    /// 再生を停止します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Stop")]
    [SuppressGCTransition]
    public static partial void AnimationPlayerStop(IntPtr player);

    /// <summary>
    /// 経過時間に応じてアニメーションの状態を更新します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <param name="deltaTime">前フレームからの経過時間 (秒)</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Update")]
    [SuppressGCTransition]
    public static partial void AnimationPlayerUpdate(IntPtr player, float deltaTime);

    /// <summary>
    /// 現在のアニメーションコマを指定されたパラメータで描画します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <param name="service">描画サービスのネイティブポインタ</param>
    /// <param name="posX">描画基準 X 座標</param>
    /// <param name="posY">描画基準 Y 座標</param>
    /// <param name="graphHandle">画像グラフィックハンドル</param>
    /// <param name="flipX">左右反転フラグ</param>
    /// <param name="scale">拡大率</param>
    /// <param name="angle">回転角度 (ラジアン)</param>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Draw")]
    [SuppressGCTransition]
    public static partial void AnimationPlayerDraw(
        IntPtr player,
        IntPtr service,
        int posX,
        int posY,
        int graphHandle,
        [MarshalAs(UnmanagedType.U1)] bool flipX,
        float scale,
        double angle);

    /// <summary>
    /// 非ループアニメーションが再生終了したかを取得します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <returns>終了した場合は <c>true</c></returns>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_IsFinished")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool AnimationPlayerIsFinished(IntPtr player);

    /// <summary>
    /// 現在のフレームコマ番号を取得します。
    /// </summary>
    /// <param name="player">アニメーションプレイヤーのネイティブポインタ</param>
    /// <returns>フレームインデックス (0 開始)</returns>
    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_GetCurrentFrameIndex")]
    [SuppressGCTransition]
    public static partial int AnimationPlayerGetCurrentFrameIndex(IntPtr player);
}
