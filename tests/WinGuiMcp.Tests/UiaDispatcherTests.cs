using WinGuiMcp.Common;
using WinGuiMcp.Platform;

namespace WinGuiMcp.Tests;

public class UiaDispatcherTests
{
    [Fact]
    public async Task WorkIsSerializedOnOneMtaThread()
    {
        using var dispatcher = new UiaDispatcher();
        var calls = Enumerable.Range(0, 20).Select(_ => dispatcher.InvokeAsync(() =>
            (Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState())));
        var results = await Task.WhenAll(calls);
        Assert.Single(results.Select(result => result.Item1).Distinct());
        Assert.All(results, result => Assert.Equal(ApartmentState.MTA, result.Item2));
    }

    [Fact]
    public async Task CancellationPreventsQueuedWorkFromRunning()
    {
        using var dispatcher = new UiaDispatcher();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var first = dispatcher.InvokeAsync(() => { entered.Set(); release.Wait(); return 1; });
        Assert.True(entered.Wait(3000));
        using var cancellation = new CancellationTokenSource();
        var ran = false;
        var second = dispatcher.InvokeAsync(() => { ran = true; return 2; }, cancellation.Token);
        cancellation.Cancel();
        release.Set();
        await first;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.False(ran);
    }

    [Fact]
    public async Task CancellationDoesNotPretendToRevokeRunningWork()
    {
        using var dispatcher = new UiaDispatcher();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var task = dispatcher.InvokeAsync(() => { entered.Set(); release.Wait(); return 7; }, cancellation.Token);
        try
        {
            Assert.True(entered.Wait(3000));
            cancellation.Cancel();
            Assert.False(task.IsCompleted);
        }
        finally { release.Set(); }
        Assert.Equal(7, await task);
    }

    [Fact]
    public async Task TimedOutNativeWorkQuarantinesWorker()
    {
        using var dispatcher = new UiaDispatcher(TimeSpan.FromMilliseconds(500));
        using var release = new ManualResetEventSlim();
        try
        {
            var first = dispatcher.InvokeAsync(() => { release.Wait(); return 1; });
            var error = await Assert.ThrowsAsync<ToolException>(() => first);
            Assert.Equal(ErrorCodes.Timeout, error.Error.Code);
            Assert.True((bool)error.Error.Details!["outcomeUnknown"]!);
            await Assert.ThrowsAsync<ToolException>(() => dispatcher.InvokeAsync(() => 2));
        }
        finally { release.Set(); }
    }
}
