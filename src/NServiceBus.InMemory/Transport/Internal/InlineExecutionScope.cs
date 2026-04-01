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

    public void BeginDispatch()
    {
        lock (lockObject)
        {
            PendingOperations++;
        }
    }

    public void CompleteDispatchSuccess()
    {
        lock (lockObject)
        {
            DecrementPendingOperations("success");

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

    public void CompleteDispatchFailure(Exception exception)
    {
        lock (lockObject)
        {
            if (terminalException == null)
            {
                terminalException = exception;
                TerminalException = exception;
            }

            DecrementPendingOperations("failure");
            if (PendingOperations == 0)
            {
                completion.TrySetException(terminalException);
            }
        }
    }

    public void CompleteDispatchCanceled(OperationCanceledException exception)
    {
        lock (lockObject)
        {
            var isFirstException = terminalException == null;

            if (isFirstException)
            {
                terminalException = exception;
                TerminalException = exception;
            }

            DecrementPendingOperations("cancellation");

            if (PendingOperations == 0)
            {
                if (isFirstException)
                {
                    completion.TrySetCanceled(exception.CancellationToken);
                }
            }
        }
    }

    void DecrementPendingOperations(string terminalState)
    {
        if (PendingOperations == 0)
        {
            throw new InvalidOperationException($"Cannot mark {terminalState}: no pending operations registered.");
        }

        PendingOperations--;
    }

    readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? terminalException;
    readonly Lock lockObject = new();
}
