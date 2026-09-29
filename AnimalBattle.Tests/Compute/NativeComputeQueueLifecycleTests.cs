using AnimalBattle.Compute;
using Xunit;

namespace AnimalBattle.Tests.Compute;

public class NativeComputeQueueLifecycleTests
{
    [Fact]
    public void NativeComputeQueue_Shutdown_ReleasesNativeMemorySafely()
    {
        NativeComputeQueue queue = new(64);
        Assert.False(queue.IsDisposed);
        Assert.True(queue.Capacity >= 64);

        queue.Dispose();
        Assert.True(queue.IsDisposed);

        // 重复释放幂等安全
        queue.Dispose();
        Assert.True(queue.IsDisposed);
    }

    [Fact]
    public void NativeComputeQueue_Shared_IsRecreatedAfterShutdown()
    {
        NativeComputeQueue queue1 = NativeComputeQueue.Shared;
        Assert.False(queue1.IsDisposed);

        NativeComputeQueue.ShutdownShared();
        Assert.True(queue1.IsDisposed);

        // 再次获取时安全懒加载重新创建
        NativeComputeQueue queue2 = NativeComputeQueue.Shared;
        Assert.False(queue2.IsDisposed);
        Assert.NotSame(queue1, queue2);
    }
}
