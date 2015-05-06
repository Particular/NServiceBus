namespace NServiceBus
{
    using System;
    using System.Threading;

    static class RequestResponse
    {
        internal class State
        {
            readonly object taskCompletionSource;

            public State(object taskCompletionSource, CancellationToken token)
            {
                this.taskCompletionSource = taskCompletionSource;
                CancellationToken = token;

                CancellationToken.Register(SetCancelled);
            }

            public Type ResponseType
            {
                get { return taskCompletionSource.GetType().GenericTypeArguments[0]; }
            }

            public CancellationToken CancellationToken { get; private set; }

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

            private void SetCancelled()
            {
                var method = taskCompletionSource.GetType().GetMethod("SetCanceled");
                method.Invoke(taskCompletionSource, new object[] { });
            }
        }
    }
}