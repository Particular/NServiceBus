namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    static class TaskEx
    {
        const string TaskIsNullExceptionMessage = "Return a Task or mark the method as async.";

        // ReSharper disable once UnusedParameter.Global
        // Used to explicitly suppress the compiler warning about
        // using the returned value from async operations
        public static void Ignore(this Task task)
        {
        }

        //TODO: remove when we update to 4.6 and can use Task.CompletedTask
        public static readonly Task CompletedTask = Task.FromResult(0);

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

        public static Task Run(Func<object, Task> func, object state) => Task.Factory.StartNew(func, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
    }
}