namespace NServiceBus.TransportTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Task extension method.")]
        public static Task SetCompleted<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, TResult result = default)
        {
            taskCompletionSource.SetResult(result);
            return taskCompletionSource.Task;
        }

        [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Task extension method.")]
        public static Task SetCompleted(this TaskCompletionSource taskCompletionSource)
        {
            taskCompletionSource.SetResult();
            return taskCompletionSource.Task;
        }
    }
}
