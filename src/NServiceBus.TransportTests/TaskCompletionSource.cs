namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;

    // Polyfill for .NET 5 and later - https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource
    class TaskCompletionSource
    {
        public Task Task => taskCompletionSource.Task;

        public void SetCanceled() => taskCompletionSource.SetCanceled();

        public void SetResult() => taskCompletionSource.SetResult(default);

        readonly TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
    }
}
