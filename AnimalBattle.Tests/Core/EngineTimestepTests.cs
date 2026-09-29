using System;
using AnimalBattle.Asset;
using Xunit;

namespace AnimalBattle.Tests.Core;

public class EngineTimestepTests
{
    [Theory]
    [InlineData(0.016f, 0.016f)]
    [InlineData(0.033f, 0.033f)]
    [InlineData(0.5f, 0.05f)]   // 拖动窗口或断点调试 500ms，必须封顶在 50ms
    [InlineData(5.0f, 0.05f)]   // 长时间卡顿 5s，必须封顶在 50ms 防止穿隧
    [InlineData(-0.01f, 0.0f)]  // 异常负时间保护
    public void Timestep_ClampPreventsSpiralOfDeath(float rawDelta, float expectedClamped)
    {
        float clamped = Math.Clamp(rawDelta, 0.0f, 0.05f);
        Assert.Equal(expectedClamped, clamped);
    }

    [Fact]
    public void AssetManager_ResetAndReinitialize_DoesNotShortCircuit()
    {
        AssetManager.ResetStateForTest();
        Assert.False(AssetManager.IsInitialized);

        AssetManager.Initialize();
        Assert.True(AssetManager.IsInitialized);

        AssetManager.ResetStateForTest();
        Assert.False(AssetManager.IsInitialized);
    }
}
