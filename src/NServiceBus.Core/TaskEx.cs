namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    static class TaskEx
    {
        const string TaskIsNullExceptionMessage = "Return a Task or mark the method as async.";

        public static readonly Task<bool> TrueTask = Task.FromResult(true);
        public static readonly Task<bool> FalseTask = Task.FromResult(false);

        public static Task<T> ThrowIfNull<T>(this Task<T> task)
        {
            if (task != null)
            {
                return task;
            }

            throw new Exception(TaskIsNullExceptionMessage);
        }

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