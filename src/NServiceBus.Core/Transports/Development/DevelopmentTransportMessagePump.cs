namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transport;

    class DevelopmentTransportMessagePump : IPushMessages
    {
        public DevelopmentTransportMessagePump(string basePath)
        {
            this.basePath = basePath;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            transactionMode = settings.RequiredTransactionMode;

            path = Path.Combine(basePath, settings.InputQueue);
            var delayedRootPath = Path.Combine(path, ".delayed");

            Directory.CreateDirectory(Path.Combine(path, ".committed"));
            Directory.CreateDirectory(delayedRootPath);

            delayedRootDirectory = new DirectoryInfo(delayedRootPath);

            purgeOnStartup = settings.PurgeOnStartup;

            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("DevelopmentTransportReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));

            delayedMessagePoller = new Timer(MoveDelayedMessagesToMainDirectory);
            return TaskEx.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            maxConcurrency = limitations.MaxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(maxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            if (purgeOnStartup)
            {
                Array.ForEach(Directory.GetFiles(path), File.Delete);
            }
            messagePumpTask = Task.Run(ProcessMessages, cancellationToken);

            delayedMessagePoller.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            await messagePumpTask
                .ConfigureAwait(false);

            while (concurrencyLimiter.CurrentCount != maxConcurrency)
            {
                await Task.Delay(50, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            concurrencyLimiter.Dispose();
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerProcessMessages()
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (NotForGracefulShutdown(ex))
                {
                    Logger.Error("File Message pump failed", ex);
                }
            }
        }

        static bool NotForGracefulShutdown(Exception ex)
        {
            return !(ex is OperationCanceledException);
        }

        async Task InnerProcessMessages()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var filesFound = false;

                foreach (var filePath in Directory.EnumerateFiles(path, "*.*"))
                {
                    filesFound = true;

                    var nativeMessageId = Path.GetFileNameWithoutExtension(filePath);

                    IDevelopmentTransportTransaction transaction;

                    if (transactionMode != TransportTransactionMode.None)
                    {
                        transaction = new DirectoryBasedTransaction(path);
                    }
                    else
                    {
                        transaction = new NoTransaction();
                    }

                    transaction.BeginTransaction(filePath);

                    await concurrencyLimiter.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    InnerProcessFile(transaction, nativeMessageId).Ignore();
                }

                if (!filesFound)
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task InnerProcessFile(IDevelopmentTransportTransaction transaction, string nativeMessageId)
        {
            try
            {
                await ProcessFile(transaction, nativeMessageId)
                    .ConfigureAwait(false);

                transaction.Complete();

                receiveCircuitBreaker.Success();
            }
            catch (Exception ex) when (NotForGracefulShutdown(ex))
            {
                await receiveCircuitBreaker.Failure(ex)
                    .ConfigureAwait(false);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        async Task ProcessFile(IDevelopmentTransportTransaction transaction, string messageId)
        {
            try
            {
                var message = await AsyncFile.ReadText(transaction.FileToProcess, cancellationToken)
                    .ConfigureAwait(false);
                string bodyPath;
                Dictionary<string, string> headers;
                ExtractMessage(out bodyPath, message, out headers);

                string ttbrString;

                if (headers.TryGetValue(Headers.TimeToBeReceived, out ttbrString))
                {
                    var ttbr = TimeSpan.Parse(ttbrString);
                    //file.move preserves create time
                    var sentTime = File.GetCreationTimeUtc(transaction.FileToProcess);

                    if (sentTime + ttbr < DateTime.UtcNow)
                    {
                        await transaction.Commit()
                            .ConfigureAwait(false);
                        return;
                    }
                }
                var tokenSource = new CancellationTokenSource();

                var context = new ContextBag();
                context.Set(transaction);

                var body = await AsyncFile.ReadBytes(bodyPath, cancellationToken)
                    .ConfigureAwait(false);

                var transportTransaction = new TransportTransaction();

                transportTransaction.Set(transaction);

                var messageContext = new MessageContext(messageId, headers, body, transportTransaction, tokenSource, new ContextBag());

                try
                {
                    await onMessage(messageContext).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    transaction.ClearPendingOutgoingOperations();
                    var processingFailures = retryCounts.AddOrUpdate(messageId, id => 1, (id, currentCount) => currentCount + 1);

                    var errorContext = new ErrorContext(exception, headers, messageId, body, transportTransaction, processingFailures);

                    var actionToTake = await onError(errorContext)
                        .ConfigureAwait(false);

                    if (actionToTake == ErrorHandleResult.RetryRequired)
                    {
                        transaction.Rollback();
                        return;
                    }
                }

                if (tokenSource.IsCancellationRequested)
                {
                    transaction.Rollback();
                    return;
                }

                await transaction.Commit()
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                transaction.Rollback();
            }
        }

        static void ExtractMessage(out string bodyPath, string message, out Dictionary<string, string> headers)
        {
            var splitIndex = message.IndexOf(Environment.NewLine, StringComparison.Ordinal);
            bodyPath = message.Substring(0, splitIndex);
            var headerStartIndex = splitIndex + Environment.NewLine.Length;
            headers = HeaderSerializer.Deserialize(message.Substring(headerStartIndex));
        }

        void MoveDelayedMessagesToMainDirectory(object state)
        {
            try
            {
                foreach (var delayDir in delayedRootDirectory.EnumerateDirectories())
                {
                    var timeToTrigger = DateTime.ParseExact(delayDir.Name, "yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);

                    if (DateTime.UtcNow >= timeToTrigger)
                    {
                        foreach (var fileInfo in delayDir.EnumerateFiles())
                        {
                            File.Move(fileInfo.FullName, Path.Combine(path, fileInfo.Name));
                        }
                    }

                    //wait a bit more so we can safely delete the dir
                    if (DateTime.UtcNow >= timeToTrigger.AddSeconds(10))
                    {
                        Directory.Delete(delayDir.FullName);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to trigger delayed messages", e);
            }
            finally
            {
                delayedMessagePoller.Change(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
            }
        }

        DirectoryInfo delayedRootDirectory;
        CancellationToken cancellationToken;
        CancellationTokenSource cancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        Task messagePumpTask;
        Func<MessageContext, Task> onMessage;
        bool purgeOnStartup;
        Func<ErrorContext, Task<ErrorHandleResult>> onError;
        ConcurrentDictionary<string, int> retryCounts = new ConcurrentDictionary<string, int>();
        string path;
        string basePath;
        RepeatedFailuresOverTimeCircuitBreaker receiveCircuitBreaker;
        Timer delayedMessagePoller;
        TransportTransactionMode transactionMode;
        int maxConcurrency;
        static ILog Logger = LogManager.GetLogger<DevelopmentTransportMessagePump>();
    }
}