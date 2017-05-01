﻿namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transport;

    class LearningTransportMessagePump : IPushMessages
    {
        public LearningTransportMessagePump(string basePath)
        {
            this.basePath = basePath;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            this.onMessage = onMessage;
            this.onError = onError;
            transactionMode = settings.RequiredTransactionMode;

            path = Path.Combine(basePath, settings.InputQueue);

            Directory.CreateDirectory(Path.Combine(path, ".committed"));

            purgeOnStartup = settings.PurgeOnStartup;

            receiveCircuitBreaker = new RepeatedFailuresOverTimeCircuitBreaker("LearningTransportReceive", TimeSpan.FromSeconds(30), ex => criticalError.Raise("Failed to receive from " + settings.InputQueue, ex));

            delayedMessagePoller = new DelayedMessagePoller(path);

            return TaskEx.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            maxConcurrency = limitations.MaxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(maxConcurrency);
            cancellationTokenSource = new CancellationTokenSource();

            cancellationToken = cancellationTokenSource.Token;

            if (purgeOnStartup)
                Array.ForEach(Directory.GetFiles(path), File.Delete);
            messagePumpTask = Task.Run(ProcessMessages, cancellationToken);

            delayedMessagePoller.Start();
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();

            await messagePumpTask
                .ConfigureAwait(false);

            while (concurrencyLimiter.CurrentCount != maxConcurrency)
                await Task.Delay(50, CancellationToken.None)
                    .ConfigureAwait(false);

            concurrencyLimiter.Dispose();
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages()
        {
            while (!cancellationToken.IsCancellationRequested)
                try
                {
                    await InnerProcessMessages()
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception exception)
                {
                    Logger.Error("File Message pump failed", exception);
                }
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

                    var transaction = GetTransaction();

                    transaction.BeginTransaction(filePath);

                    await concurrencyLimiter.WaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    InnerProcessFile(transaction, nativeMessageId).Ignore();
                }

                if (!filesFound)
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        ILearningTransportTransaction GetTransaction()
        {
            if (transactionMode == TransportTransactionMode.None)
                return new NoTransaction(path);
            var immediateDispatch = transactionMode == TransportTransactionMode.ReceiveOnly;
            return new DirectoryBasedTransaction(path, immediateDispatch);
        }

        async Task InnerProcessFile(ILearningTransportTransaction transaction, string nativeMessageId)
        {
            try
            {
                await ProcessFile(transaction, nativeMessageId)
                    .ConfigureAwait(false);

                transaction.Complete();

                receiveCircuitBreaker.Success();
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                await receiveCircuitBreaker.Failure(ex)
                    .ConfigureAwait(false);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        async Task ProcessFile(ILearningTransportTransaction transaction, string messageId)
        {
            try
            {
                var message = await AsyncFile.ReadText(transaction.FileToProcess)
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
                    await onMessage(messageContext)
                        .ConfigureAwait(false);
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
        DelayedMessagePoller delayedMessagePoller;
        TransportTransactionMode transactionMode;
        int maxConcurrency;
        static ILog Logger = LogManager.GetLogger<LearningTransportMessagePump>();
    }
}