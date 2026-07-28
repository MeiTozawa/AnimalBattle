using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// 音声・SE・BGM の再生および制御操作を提供する静的クラス。
/// </summary>
public static partial class AudioManager
{
    private const string _dllName = "AnimalBattleCore.dll";

    /// <summary>
    /// メモリ上のサウンドハンドルを再生します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    /// <param name="playType">再生タイプ</param>
    /// <param name="topPositionFlag">最初から再生する場合は true</param>
    /// <returns>成功時は 0</returns>
    public static int PlaySound(int handle, int playType, bool topPositionFlag = true)
    {
        return NativePlaySoundMem(handle, playType, topPositionFlag);
    }

    /// <summary>
    /// メモリ上のサウンド再生を停止します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    public static void StopSound(int handle)
    {
        NativeStopSoundMem(handle);
    }

    /// <summary>
    /// サウンドの音量を変更します。
    /// </summary>
    /// <param name="volumePercent">音量パーセンテージ (0-100)</param>
    /// <param name="handle">サウンドハンドル</param>
    /// <returns>成功時は 0</returns>
    public static int ChangeVolume(int volumePercent, int handle)
    {
        return NativeChangeVolumeSoundMem(volumePercent, handle);
    }

    [LibraryImport(_dllName, EntryPoint = "Engine_PlaySoundMem")]
    [SuppressGCTransition]
    private static partial int NativePlaySoundMem(int handle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    [LibraryImport(_dllName, EntryPoint = "Engine_StopSoundMem")]
    [SuppressGCTransition]
    private static partial void NativeStopSoundMem(int handle);

    [LibraryImport(_dllName, EntryPoint = "Engine_ChangeVolumeSoundMem")]
    [SuppressGCTransition]
    private static partial int NativeChangeVolumeSoundMem(int volumePercent, int handle);
}
