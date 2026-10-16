using System.Collections.Concurrent;

namespace Awaken.Core;

/// <summary>
/// 唤醒引擎。ES_CONTINUOUS 绑定在调用线程上，因此所有 SetThreadExecutionState
/// 调用必须发生在同一个专用线程：UI 线程把目标状态投入队列，由引擎线程串行应用。
/// </summary>
internal sealed class AwakeManager : IDisposable
{
    private readonly BlockingCollection<ExecutionState> _stateQueue = new();
    private readonly Thread _thread;
    private bool _disposed;

    public AwakeManager()
    {
        _thread = new Thread(Run)
        {
            Name = "Awaken.ExecutionState",
            IsBackground = false,
        };
        _thread.Start();
    }

    /// <summary>切换唤醒模式。非阻塞，实际状态由引擎线程异步应用。</summary>
    public void SetMode(AwakeMode mode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stateQueue.Add(Translate(mode));
    }

    private static ExecutionState Translate(AwakeMode mode) => mode switch
    {
        AwakeMode.KeepAwake => ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED,
        AwakeMode.KeepDisplayOn => ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED | ExecutionState.ES_DISPLAY_REQUIRED,
        _ => ExecutionState.ES_CONTINUOUS,
    };

    private void Run()
    {
        foreach (ExecutionState state in _stateQueue.GetConsumingEnumerable())
        {
            NativeMethods.SetThreadExecutionState(state);
        }

        // 引擎线程退出前清除持续请求，避免状态残留（线程终止时系统也会自动清除）。
        NativeMethods.SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stateQueue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(2));
        _stateQueue.Dispose();
    }
}
