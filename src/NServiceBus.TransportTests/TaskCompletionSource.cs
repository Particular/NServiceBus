namespace NServiceBus.TransportTests
{
    using System;
    using System.Threading.Tasks;

    // Polyfill for .NET 5 and later - https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource
    public class TaskCompletionSource
    {
        public Task Task => taskCompletionSource.Task;

        public void SetResult() => taskCompletionSource.SetResult(default);

        public bool TrySetException(Exception exception) => taskCompletionSource.TrySetException(exception);

        readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
    }
}
