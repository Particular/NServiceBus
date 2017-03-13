namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transport;

    class DevelopmentTransportMessagePump : IPushMessages
    {
        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            this.onMessage = onMessage;
            this.onError = onError;

            path = Path.Combine("c:\\bus", settings.InputQueue);

            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.Combine(path, ".committed"));

            purgeOnStartup = settings.PurgeOnStartup;

            return TaskEx.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            if (purgeOnStartup)
            {
                Array.ForEach(Directory.GetFiles(path), File.Delete);
            }

            messagePumpTask = Task.Factory.StartNew(() => ProcessMessages(), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            // ReSharper disable once MethodSupportsCancellation
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var allTasks = runningReceiveTasks.Values.Concat(new[]
            {
                messagePumpTask
            });
            var finishedTask = await Task.WhenAny(Task.WhenAll(allTasks), timeoutTask).ConfigureAwait(false);

            if (finishedTask.Equals(timeoutTask))
            {
                Logger.Error("The message pump failed to stop with in the time allowed(30s)");
            }

            concurrencyLimiter.Dispose();
            runningReceiveTasks.Clear();
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            try
            {
                await InnerProcessMessages().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // For graceful shutdown purposes
            }
            catch (Exception ex)
            {
                Logger.Error("File Message pump failed", ex);
                //await peekCircuitBreaker.Failure(ex).ConfigureAwait(false);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await ProcessMessages().ConfigureAwait(false);
            }
        }

        async Task InnerProcessMessages()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var filesFound = false;

                foreach (var filePath in Directory.EnumerateFiles(path, "*.*"))
                {
                    filesFound = true;

                    var nativeMessageId = Path.GetFileNameWithoutExtension(filePath);

                    var transaction = new DirectoryBasedTransaction(path);

                    transaction.BeginTransaction(filePath);

                    await concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessFile(transaction, nativeMessageId).ConfigureAwait(false);

                            transaction.Complete();
                        }
                        finally
                        {
                            concurrencyLimiter.Release();
                        }
                    }, cancellationToken);

                    task.ContinueWith(t =>
                    {
                        Task toBeRemoved;
                        runningReceiveTasks.TryRemove(t, out toBeRemoved);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                        .Ignore();

                    runningReceiveTasks.AddOrUpdate(task, task, (k, v) => task)
                        .Ignore();
                }

                if (!filesFound)
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task ProcessFile(DirectoryBasedTransaction transaction, string messageId)
        {
            try
            {
                var message = File.ReadAllLines(transaction.FileToProcess);
                var bodyPath = message.First();
                var headers = HeaderSerializer.FromString(string.Join("", message.Skip(1)));

                string ttbrString;

                if (headers.TryGetValue(Headers.TimeToBeReceived, out ttbrString))
                {
                    var ttbr = TimeSpan.Parse(ttbrString);
                    var sentTime = File.GetCreationTimeUtc(transaction.FileToProcess); //file.move preserves create time

                    if (sentTime + ttbr < DateTime.UtcNow)
                    {
                        transaction.Commit();
                        return;
                    }
                }
                var tokenSource = new CancellationTokenSource();

                using (var bodyStream = new FileStream(bodyPath, FileMode.Open))
                {
                    var context = new ContextBag();
                    context.Set(transaction);

                    var body = await ReadStream(bodyStream).ConfigureAwait(false);

                    var transportTransaction = new TransportTransaction();

                    transportTransaction.Set(transaction);

                    var messageContext = new MessageContext(messageId, headers, body, transportTransaction, tokenSource, new ContextBag());

                    try
                    {
                        await onMessage(messageContext).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var immediateProcessinFailures = retryCounts.AddOrUpdate(messageId, id => 1, (id, currentCount) => currentCount + 1);

                        var errorContext = new ErrorContext(ex, headers, messageId, body, transportTransaction, immediateProcessinFailures);
                        var actionToTake = await onError(errorContext).ConfigureAwait(false);

                        if (actionToTake == ErrorHandleResult.RetryRequired)
                        {
                            transaction.Rollback();
                            return;
                        }

                    }

                }

                if (tokenSource.IsCancellationRequested)
                {
                    transaction.Rollback();
                    return;
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
            }
        }

        static async Task<byte[]> ReadStream(Stream bodyStream)
        {
            bodyStream.Seek(0, SeekOrigin.Begin);
            var length = (int)bodyStream.Length;
            var body = new byte[length];
            await bodyStream.ReadAsync(body, 0, length).ConfigureAwait(false);
            return body;
        }

        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;

        Task messagePumpTask;

        string path;
        Func<MessageContext, Task> onMessage;
        bool purgeOnStartup;
        ConcurrentDictionary<Task, Task> runningReceiveTasks;
        static ILog Logger = LogManager.GetLogger<DevelopmentTransportMessagePump>();
        Func<ErrorContext, Task<ErrorHandleResult>> onError;

        ConcurrentDictionary<string, int> retryCounts = new ConcurrentDictionary<string, int>();
    }
}