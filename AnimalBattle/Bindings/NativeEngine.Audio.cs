using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// メモリ上のサウンドハンドルを再生します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    /// <param name="playType">再生タイプ（DX_PLAYTYPE_*）</param>
    /// <param name="topPositionFlag">最初から再生する場合は true</param>
    /// <return>成功時は 0</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_PlaySoundMem")]
    [SuppressGCTransition]
    public static partial int PlaySoundMem(int handle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    /// <summary>
    /// メモリ上のサウンド再生を停止します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    [LibraryImport(_dllName, EntryPoint = "Engine_StopSoundMem")]
    [SuppressGCTransition]
    public static partial void StopSoundMem(int handle);

    /// <summary>
    /// サウンドの音量を変更します。
    /// </summary>
    /// <param name="volumePercent">音量パーセンテージ (0-100)</param>
    /// <param name="handle">サウンドハンドル</param>
    /// <return>成功時は 0</return>
    [LibraryImport(_dllName, EntryPoint = "Engine_ChangeVolumeSoundMem")]
    [SuppressGCTransition]
    public static partial int ChangeVolumeSoundMem(int volumePercent, int handle);
}
