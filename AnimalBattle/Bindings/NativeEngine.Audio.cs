using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    [LibraryImport(DllName, EntryPoint = "Engine_PlaySoundMem")]
    [SuppressGCTransition]
    public static partial int PlaySoundMem(int handle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    [LibraryImport(DllName, EntryPoint = "Engine_StopSoundMem")]
    [SuppressGCTransition]
    public static partial void StopSoundMem(int handle);

    [LibraryImport(DllName, EntryPoint = "Engine_ChangeVolumeSoundMem")]
    [SuppressGCTransition]
    public static partial int ChangeVolumeSoundMem(int volumePercent, int handle);
}
