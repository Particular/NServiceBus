namespace NServiceBus.TransportTests
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        [SuppressMessage("Code", "PCR0019:A task-returning method should have a CancellationToken parameter or a parameter implementing ICancellableContext", Justification = "Task extension method.")]
        public static Task SetCompleted<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, TResult result = default)
        {
            taskCompletionSource.SetResult(result);
            return taskCompletionSource.Task;
        }

        [SuppressMessage("Code", "PCR0019:A task-returning method should have a CancellationToken parameter or a parameter implementing ICancellableContext", Justification = "Task extension method.")]
        public static Task SetCompleted(this TaskCompletionSource taskCompletionSource)
        {
            taskCompletionSource.SetResult();
            return taskCompletionSource.Task;
        }
    }
}
