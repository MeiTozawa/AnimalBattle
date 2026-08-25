using AnimalBattle.Bindings;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// Boss1 の軌道計算 native extension への境界です。
/// State_Module はこの型を参照せず、移動 command だけを発行します。
/// </summary>
public sealed class Boss1NativeTrajectoryPort
{
    /// <summary>Boss1 の浮遊軌道を native extension で計算します。</summary>
    public bool TryCalculateHover(
        in Vec2f currentPosition,
        in Vec2f basePosition,
        in Vec2f playerPosition,
        float hoverTimer,
        float hoverSpeedX,
        float hoverSpeedY,
        float hoverAmplitudeX,
        float hoverAmplitudeY,
        float playerPursuitWeight,
        float hoverLerpSpeed,
        float deltaTime,
        out Vec2f position)
    {
        position = default;
        if (!AreFinite(
                currentPosition,
                basePosition,
                playerPosition,
                hoverTimer,
                hoverSpeedX,
                hoverSpeedY,
                hoverAmplitudeX,
                hoverAmplitudeY,
                playerPursuitWeight,
                hoverLerpSpeed,
                deltaTime)
            || deltaTime < 0.0f)
        {
            return false;
        }

        try
        {
            NativeExtensionBindings.Extension_CalculateBossHoverPosition(
                in currentPosition,
                in basePosition,
                in playerPosition,
                hoverTimer,
                hoverSpeedX,
                hoverSpeedY,
                hoverAmplitudeX,
                hoverAmplitudeY,
                playerPursuitWeight,
                hoverLerpSpeed,
                deltaTime,
                out position);
            return IsFinite(position);
        }
        catch
        {
            position = default;
            return false;
        }
    }

    /// <summary>Boss1 の DiveRam 軌道を native extension で計算します。</summary>
    public static bool TryCalculateDiveRam(
        in Vec2f startPosition,
        in Vec2f targetPosition,
        float stateTimer,
        float stateDuration,
        EaseType easeType,
        out Vec2f position)
    {
        position = default;
        if (!AreFinite(startPosition, targetPosition, stateTimer, stateDuration)
            || stateTimer < 0.0f
            || stateDuration <= 0.0f)
        {
            return false;
        }

        try
        {
            NativeExtensionBindings.Extension_CalculateBossDiveRamPosition(
                in startPosition,
                in targetPosition,
                stateTimer,
                stateDuration,
                easeType,
                out position);
            return IsFinite(position);
        }
        catch
        {
            position = default;
            return false;
        }
    }

    /// <summary>Boss1 の基準位置への帰還軌道を native extension で計算します。</summary>
    public bool TryCalculateReturn(
        in Vec2f currentPosition,
        in Vec2f targetBasePosition,
        float returnSpeed,
        float deltaTime,
        out Vec2f position)
    {
        position = default;
        if (!AreFinite(currentPosition, targetBasePosition, returnSpeed, deltaTime)
            || returnSpeed < 0.0f
            || deltaTime < 0.0f)
        {
            return false;
        }

        try
        {
            NativeExtensionBindings.Extension_CalculateBossReturnPosition(
                in currentPosition,
                in targetBasePosition,
                returnSpeed,
                deltaTime,
                out position);
            return IsFinite(position);
        }
        catch
        {
            position = default;
            return false;
        }
    }

    private static bool AreFinite(
        in Vec2f first,
        in Vec2f second,
        in Vec2f third,
        float firstValue,
        float secondValue,
        float thirdValue,
        float fourthValue,
        float fifthValue,
        float sixthValue,
        float seventhValue,
        float eighthValue)
    {
        return IsFinite(first)
            && IsFinite(second)
            && IsFinite(third)
            && float.IsFinite(firstValue)
            && float.IsFinite(secondValue)
            && float.IsFinite(thirdValue)
            && float.IsFinite(fourthValue)
            && float.IsFinite(fifthValue)
            && float.IsFinite(sixthValue)
            && float.IsFinite(seventhValue)
            && float.IsFinite(eighthValue);
    }

    private static bool AreFinite(in Vec2f first, in Vec2f second, float firstValue, float secondValue)
    {
        return IsFinite(first)
            && IsFinite(second)
            && float.IsFinite(firstValue)
            && float.IsFinite(secondValue);
    }

    private static bool IsFinite(in Vec2f value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }
}
