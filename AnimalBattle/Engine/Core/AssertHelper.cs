using System.Runtime.CompilerServices;

namespace AnimalBattle.Core;

public static class AssertHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FailWithDefault<T>(string message, T defaultValue = default!)
    {
        Debug.Fail(message);
        return defaultValue;
    }
}