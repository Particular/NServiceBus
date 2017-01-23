namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        public static Task Timebox(this IEnumerable<Task> tasks, TimeSpan timeoutAfter, string messageWhenTimeboxReached)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            var tokenSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(timeoutAfter);
            var registration = tokenSource.Token.Register(s =>
            {
                var tcs = (TaskCompletionSource<object>) s;
                tcs.TrySetException(new TimeoutException(messageWhenTimeboxReached));
            }, taskCompletionSource);

            Task.WhenAll(tasks)
                .ContinueWith((t, s) =>
                {
                    var state = (Tuple<TaskCompletionSource<object>, CancellationTokenSource, CancellationTokenRegistration>) s;
                    var source = state.Item2;
                    var reg = state.Item3;
                    var tcs = state.Item1;

                    if (t.IsFaulted && t.Exception != null)
                    {
                        tcs.TrySetException(t.Exception.InnerException);
                    }

                    if (t.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                    }

                    if (t.IsCompleted)
                    {
                        tcs.TrySetResult(null);
                    }

                    reg.Dispose();
                    source.Dispose();
                }, Tuple.Create(taskCompletionSource, tokenSource, registration), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return taskCompletionSource.Task;
        }
    }
}