using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// 効果音および BGM のメモリ再生・音量変更を管理するマネージャークラス。
/// </summary>
public static class AudioManager
{
    public const int DxPlayTypeNormal = 0;
    public const int DxPlayTypeBack = 1;
    public const int DxPlayTypeLoop = 2;

    /// <summary>
    /// メモリ上のサウンドハンドルを再生します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    /// <param name="playType">再生タイプ (DxPlayTypeNormal, DxPlayTypeBack, DxPlayTypeLoop)</param>
    /// <param name="topPositionFlag">先頭から再生するかどうか</param>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int PlaySoundMem(int handle, int playType = DxPlayTypeBack, bool topPositionFlag = true)
    {
        return NativeBindings.Engine_PlaySoundMem(handle, playType, topPositionFlag);
    }

    /// <summary>
    /// 再生中のサウンドを停止します。
    /// </summary>
    /// <param name="handle">サウンドハンドル</param>
    public static void StopSoundMem(int handle)
    {
        NativeBindings.Engine_StopSoundMem(handle);
    }

    /// <summary>
    /// サウンドハンドルの再生音量を変更します。
    /// </summary>
    /// <param name="volumePercent">音量パーセンテージ (0～100)</param>
    /// <param name="handle">サウンドハンドル</param>
    /// <returns>成功時 0、失敗時 -1</returns>
    public static int ChangeVolumeSoundMem(int volumePercent, int handle)
    {
        int clamped = Math.Clamp(volumePercent, 0, 100);
        return NativeBindings.Engine_ChangeVolumeSoundMem(clamped, handle);
    }
}
