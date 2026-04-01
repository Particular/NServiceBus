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

            if (PendingOperations == 0)
            {
                if (terminalException == null)
                {
                    completion.TrySetResult();
                }
                else
                {
                    completion.TrySetException(terminalException);
                }
            }
        }
    }

    public void MarkTerminalFailure(Exception exception)
    {
        lock (lockObject)
        {
            if (terminalException == null)
            {
                terminalException = exception;
                TerminalException = exception;
            }

            PendingOperations--;
            if (PendingOperations == 0)
            {
                completion.TrySetException(terminalException);
            }
        }
    }

    public void MarkCanceled(OperationCanceledException exception)
    {
        lock (lockObject)
        {
            var isFirstException = terminalException == null;

            if (isFirstException)
            {
                terminalException = exception;
                TerminalException = exception;
            }

            PendingOperations--;

            if (PendingOperations == 0)
            {
                if (isFirstException)
                {
                    completion.TrySetCanceled(exception.CancellationToken);
                }
            }
        }
    }

    readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? terminalException;
    readonly Lock lockObject = new();
}
