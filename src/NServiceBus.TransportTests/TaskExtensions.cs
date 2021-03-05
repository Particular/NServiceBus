namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        public static Task SetCompleted<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, TResult result = default)
        {
            taskCompletionSource.SetResult(result);
            return taskCompletionSource.Task;
        }
        public static Task SetCompleted(this TaskCompletionSource taskCompletionSource)
        {
            taskCompletionSource.SetResult();
            return taskCompletionSource.Task;
        }
    }
}
