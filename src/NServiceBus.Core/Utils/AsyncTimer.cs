namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class AsyncTimer : IAsyncTimer
    {
        public void Start(Func<Task> callback, TimeSpan interval, Action<Exception> errorCallback)
        {
            tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(interval, token).ConfigureAwait(false);
                        await callback().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // no-op
                    }
                    catch (Exception ex)
                    {
                        errorCallback(ex);
                    }
                }
            }, CancellationToken.None);
        }

        public Task Stop()
        {
            if (tokenSource == null)
            {
                return Task.CompletedTask;
            }

            tokenSource.Cancel();
            tokenSource.Dispose();

            return task ?? Task.CompletedTask;
        }

        Task task;
        CancellationTokenSource tokenSource;
    }
}