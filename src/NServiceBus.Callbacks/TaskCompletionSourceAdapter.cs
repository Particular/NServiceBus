namespace NServiceBus
{
    using System;

    class TaskCompletionSourceAdapter
    {
        readonly object taskCompletionSource;

        public TaskCompletionSourceAdapter(object taskCompletionSource)
        {
            this.taskCompletionSource = taskCompletionSource;
        }

        public Type ResponseType
        {
            get { return taskCompletionSource.GetType().GenericTypeArguments[0]; }
        }

        public void SetResult(object result)
        {
            var method = taskCompletionSource.GetType().GetMethod("SetResult");
            method.Invoke(taskCompletionSource, new[]
            {
                result
            });
        }

        public void SetException(Exception exception)
        {
            var methodSetException = taskCompletionSource.GetType().GetMethod("SetException");
            methodSetException.Invoke(taskCompletionSource, new object[]
            {
                exception
            });
        }

        public void SetCancelled()
        {
            var method = taskCompletionSource.GetType().GetMethod("SetCanceled");
            method.Invoke(taskCompletionSource, new object[] { });
        }
    }
}