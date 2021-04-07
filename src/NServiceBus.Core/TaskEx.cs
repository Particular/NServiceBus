namespace NServiceBus
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    static class TaskEx
    {
        const string TaskIsNullExceptionMessage = "Return a Task or mark the method as async.";

        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);

        [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Task wrapper.")]
        public static Task<T> ThrowIfNull<T>(this Task<T> task)
        {
            if (task != null)
            {
                return task;
            }

            throw new Exception(TaskIsNullExceptionMessage);
        }

        [SuppressMessage("Code", "PS0018:A task-returning method should have a CancellationToken parameter unless it has a parameter implementing ICancellableContext", Justification = "Task wrapper.")]
        public static Task ThrowIfNull(this Task task)
        {
            if (task != null)
            {
                return task;
            }

            throw new Exception(TaskIsNullExceptionMessage);
        }
    }
}