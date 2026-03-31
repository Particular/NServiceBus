namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class InlineExecutionScope(Guid rootExecutionId)
{
    public Guid RootExecutionId { get; } = rootExecutionId;
    public Task Completion => completion.Task;
    public Exception? TerminalException { get; private set; }
    public int PendingOperations { get; private set; }

    public void RegisterDispatch()
    {
        lock (lockObject)
        {
            PendingOperations++;
        }
    }

    public void MarkSuccess()
    {
        lock (lockObject)
        {
            if (PendingOperations == 0)
            {
                throw new InvalidOperationException("Cannot mark success: no pending operations registered.");
            }

            PendingOperations--;

            if (PendingOperations == 0 && terminalException == null)
            {
                completion.TrySetResult();
            }
        }
    }

    public void MarkTerminalFailure(Exception exception)
    {
        lock (lockObject)
        {
            if (terminalException != null)
            {
                return;
            }

            terminalException = exception;
            TerminalException = exception;

            MarkCompletedAndDecrement();
        }
    }

    public void MarkCanceled(OperationCanceledException exception)
    {
        lock (lockObject)
        {
            if (terminalException != null)
            {
                return;
            }

            terminalException = exception;
            TerminalException = exception;
            completion.TrySetCanceled(exception.CancellationToken);
        }
    }

    void MarkCompletedAndDecrement()
    {
        PendingOperations--;

        if (PendingOperations == 0)
        {
            if (terminalException != null)
            {
                completion.TrySetException(terminalException);
            }
            else
            {
                completion.TrySetResult();
            }
        }
    }

    readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? terminalException;
    readonly Lock lockObject = new();
}