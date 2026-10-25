using System.Collections.Concurrent;
using WinGuiMcp.Common;

namespace WinGuiMcp.Platform;

/// <summary>Serializes UIA calls; a timed-out running provider call quarantines the worker.</summary>
public sealed class UiaDispatcher : IDisposable
{
    private static readonly Lazy<UiaDispatcher> Default = new(() => new UiaDispatcher());
    public static UiaDispatcher Shared => Default.Value;
    private readonly BlockingCollection<System.Action> _queue = new(256);
    private readonly Thread _thread;
    private readonly TimeSpan _timeout;
    private int _disposed;
    private int _quarantined;

    public UiaDispatcher(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(15);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        _thread = new Thread(() =>
        {
            try { foreach (var action in _queue.GetConsumingEnumerable()) action(); }
            finally { _queue.Dispose(); }
        }) { IsBackground = true, Name = "WinGuiMcp.UIA" };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }

    public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposed) != 0) return Task.FromException<T>(new ObjectDisposedException(nameof(UiaDispatcher)));
        if (Volatile.Read(ref _quarantined) != 0) return Task.FromException<T>(Unavailable());
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<T>(cancellationToken);
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new object();
        var started = false;
        try
        {
            if (!_queue.TryAdd(() =>
            {
                lock (gate)
                {
                    if (completion.Task.IsCompleted) return;
                    if (Volatile.Read(ref _quarantined) != 0) { completion.TrySetException(Unavailable()); return; }
                    if (cancellationToken.IsCancellationRequested) { completion.TrySetCanceled(cancellationToken); return; }
                    started = true;
                }
                try
                {
                    var result = action();
                    lock (gate) completion.TrySetResult(result);
                }
                catch (OperationCanceledException) { lock (gate) completion.TrySetCanceled(cancellationToken); }
                catch (Exception error) { lock (gate) completion.TrySetException(error); }
            })) completion.TrySetException(new ToolException(ErrorCodes.Timeout, "UIA queue is full; no action was started."));
        }
        catch (InvalidOperationException) { completion.TrySetException(new ObjectDisposedException(nameof(UiaDispatcher))); }
        return ObserveAsync();

        async Task<T> ObserveAsync()
        {
            using var registration = cancellationToken.Register(() =>
            {
                lock (gate) { if (!started) completion.TrySetCanceled(cancellationToken); }
            });
            using var timer = new Timer(_ =>
            {
                lock (gate)
                {
                    if (completion.Task.IsCompleted) return;
                    if (started) Interlocked.Exchange(ref _quarantined, 1);
                    completion.TrySetException(new ToolException(ErrorCodes.Timeout,
                        started ? "UIA provider timed out; native outcome is unknown. Restart the server and inspect state before retrying." : "UIA action timed out in the queue; it was not started.",
                        new Dictionary<string, object?> { ["outcomeUnknown"] = started }));
                }
            }, null, _timeout, Timeout.InfiniteTimeSpan);
            return await completion.Task.ConfigureAwait(false);
        }
    }

    private static ToolException Unavailable() => new(ErrorCodes.Timeout, "UIA worker is quarantined after a provider timeout. Restart the server and inspect state before retrying.",
        new Dictionary<string, object?> { ["workerUnavailable"] = true, ["outcomeUnknown"] = true });

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _queue.CompleteAdding();
        if (Thread.CurrentThread != _thread) _thread.Join(TimeSpan.FromSeconds(2));
    }
}
