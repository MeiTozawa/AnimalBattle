using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    [LibraryImport(DllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int LoadGraph(string path);

    [LibraryImport(DllName, EntryPoint = "Engine_DeleteGraph")]
    public static partial int DeleteGraph(int handle);
}
