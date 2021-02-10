﻿namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transport;

    class LearningTransportMessagePump : IMessageReceiver
    {
        public LearningTransportMessagePump(string id,
            string basePath,
            Action<string, Exception, CancellationToken> criticalErrorAction,
            ISubscriptionManager subscriptionManager,
            ReceiveSettings receiveSettings,
            TransportTransactionMode transactionMode)
        {
            Id = id;
            this.basePath = basePath;
            this.criticalErrorAction = criticalErrorAction;
            Subscriptions = subscriptionManager;
            this.receiveSettings = receiveSettings;
            this.transactionMode = transactionMode;
        }

        public void Init()
        {
            PathChecker.ThrowForBadPath(receiveSettings.ReceiveAddress, "InputQueue");

            messagePumpBasePath = Path.Combine(basePath, receiveSettings.ReceiveAddress);
            bodyDir = Path.Combine(messagePumpBasePath, BodyDirName);
            delayedDir = Path.Combine(messagePumpBasePath, DelayedDirName);

            pendingTransactionDir = Path.Combine(messagePumpBasePath, PendingDirName);
            committedTransactionDir = Path.Combine(messagePumpBasePath, CommittedDirName);

            if (receiveSettings.PurgeOnStartup)
            {
                if (Directory.Exists(messagePumpBasePath))
                {
                    Directory.Delete(messagePumpBasePath, true);
                }
            }

            delayedMessagePoller = new DelayedMessagePoller(messagePumpBasePath, delayedDir);
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken)
        {
            this.onMessage = onMessage;
            this.onError = onError;

            Init();

            // use concurrency 1 if the user hasn't explicitly configured a concurrency value
            maxConcurrency = limitations == PushRuntimeSettings.Default ? 1 : limitations.MaxConcurrency;
            concurrencyLimiter = new SemaphoreSlim(maxConcurrency);

            RecoverPendingTransactions();

            EnsureDirectoriesExists();

            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken)
        {
            stopRequestedCancellationTokenSource = new CancellationTokenSource();

            messagePumpTask = Task.Run(() => ProcessMessages(stopRequestedCancellationTokenSource.Token), stopRequestedCancellationTokenSource.Token);

            delayedMessagePoller.Start();

            return Task.CompletedTask;
        }

        public async Task StopReceive(CancellationToken cancellationToken)
        {
            stopRequestedCancellationTokenSource?.Cancel();

            delayedMessagePoller.Stop();

            await messagePumpTask
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested && concurrencyLimiter.CurrentCount != maxConcurrency)
            {
                await Task.Delay(50, cancellationToken)
                    .ConfigureAwait(false);
            }

            concurrencyLimiter.Dispose();
        }

        public ISubscriptionManager Subscriptions { get; }

        public string Id { get; }

        void RecoverPendingTransactions()
        {
            if (transactionMode != TransportTransactionMode.None)
            {
                DirectoryBasedTransaction.RecoverPartiallyCompletedTransactions(messagePumpBasePath, PendingDirName, CommittedDirName);
            }
            else
            {
                if (!Directory.Exists(pendingTransactionDir))
                {
                    return;
                }

                try
                {
                    Directory.Delete(pendingTransactionDir, true);
                }
                catch (Exception e)
                {
                    log.Debug($"Unable to delete pending transaction directory '{pendingTransactionDir}'.", e);
                }
            }
        }

        void EnsureDirectoriesExists()
        {
            Directory.CreateDirectory(messagePumpBasePath);
            Directory.CreateDirectory(bodyDir);
            Directory.CreateDirectory(delayedDir);
            Directory.CreateDirectory(pendingTransactionDir);

            if (transactionMode != TransportTransactionMode.None)
            {
                Directory.CreateDirectory(committedTransactionDir);
            }
        }

        [DebuggerNonUserCode]
        async Task ProcessMessages(CancellationToken stopRequestedCancellationToken)
        {
            while (!stopRequestedCancellationToken.IsCancellationRequested)
            {
                try
                {
                    await InnerProcessMessages(stopRequestedCancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    criticalErrorAction("Failure to process messages", ex, CancellationToken.None);
                }
            }
        }

        async Task InnerProcessMessages(CancellationToken stopRequestedCancellationToken)
        {
            log.Debug($"Started polling for new messages in {messagePumpBasePath}");

            while (!stopRequestedCancellationToken.IsCancellationRequested)
            {
                var filesFound = false;

                foreach (var filePath in Directory.EnumerateFiles(messagePumpBasePath, "*.*"))
                {
                    filesFound = true;

                    var nativeMessageId = Path.GetFileNameWithoutExtension(filePath).Replace(".metadata", "");

                    await concurrencyLimiter.WaitAsync(stopRequestedCancellationToken)
                        .ConfigureAwait(false);

                    ILearningTransportTransaction transaction;

                    try
                    {
                        transaction = GetTransaction();

                        var ableToLockFile = await transaction.BeginTransaction(filePath, stopRequestedCancellationToken).ConfigureAwait(false);

                        if (!ableToLockFile)
                        {
                            log.Debug($"Unable to lock file {filePath}({transaction.FileToProcess})");
                            concurrencyLimiter.Release();
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Failed to begin transaction {filePath}", ex);

                        concurrencyLimiter.Release();
                        throw;
                    }

                    _ = ProcessFileAndComplete(transaction, filePath, nativeMessageId, stopRequestedCancellationToken);
                }

                if (!filesFound)
                {
                    await Task.Delay(10, stopRequestedCancellationToken).ConfigureAwait(false);
                }
            }
        }

        ILearningTransportTransaction GetTransaction()
        {
            if (transactionMode == TransportTransactionMode.None)
            {
                return new NoTransaction(messagePumpBasePath, PendingDirName);
            }

            return new DirectoryBasedTransaction(messagePumpBasePath, PendingDirName, CommittedDirName, Guid.NewGuid().ToString());
        }

        async Task ProcessFileAndComplete(ILearningTransportTransaction transaction, string filePath, string messageId, CancellationToken stopRequestedCancellationToken)
        {
            try
            {
                await ProcessFile(transaction, messageId, stopRequestedCancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Completing processing for {filePath}({transaction.FileToProcess}).");
                }

                try
                {
                    var wasCommitted = transaction.Complete();

                    if (wasCommitted)
                    {
                        File.Delete(Path.Combine(bodyDir, messageId + BodyFileSuffix));
                    }
                }
                catch (Exception ex)
                {
                    log.Debug($"Failure while trying to complete receive transaction for  {filePath}({transaction.FileToProcess})" + filePath, ex);
                }

                concurrencyLimiter.Release();
            }
        }

        async Task ProcessFile(ILearningTransportTransaction transaction, string messageId, CancellationToken stopRequestedCancellationToken)
        {
            var message = await AsyncFile.ReadText(transaction.FileToProcess, stopRequestedCancellationToken)
                    .ConfigureAwait(false);

            var bodyPath = Path.Combine(bodyDir, $"{messageId}{BodyFileSuffix}");
            var headers = HeaderSerializer.Deserialize(message);

            if (headers.TryGetValue(LearningTransportHeaders.TimeToBeReceived, out var ttbrString))
            {
                headers.Remove(LearningTransportHeaders.TimeToBeReceived);

                var ttbr = TimeSpan.Parse(ttbrString);

                //file.move preserves create time
                var sentTime = File.GetCreationTimeUtc(transaction.FileToProcess);

                var utcNow = DateTime.UtcNow;
                if (sentTime + ttbr < utcNow)
                {
                    await transaction.Commit(stopRequestedCancellationToken)
                        .ConfigureAwait(false);
                    log.InfoFormat("Dropping message '{0}' as the specified TimeToBeReceived of '{1}' expired since sending the message at '{2:O}'. Current UTC time is '{3:O}'", messageId, ttbrString, sentTime, utcNow);
                    return;
                }
            }

            var body = await AsyncFile.ReadBytes(bodyPath, stopRequestedCancellationToken)
                .ConfigureAwait(false);

            var transportTransaction = new TransportTransaction();

            if (transactionMode == TransportTransactionMode.SendsAtomicWithReceive)
            {
                transportTransaction.Set(transaction);
            }

            var messageContext = new MessageContext(messageId, headers, body, transportTransaction, new ContextBag());

            try
            {
                await onMessage(messageContext, stopRequestedCancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                transaction.Rollback();

                return;
            }
            catch (Exception exception)
            {
                transaction.ClearPendingOutgoingOperations();
                var processingFailures = retryCounts.AddOrUpdate(messageId, id => 1, (id, currentCount) => currentCount + 1);

                headers = HeaderSerializer.Deserialize(message);
                headers.Remove(LearningTransportHeaders.TimeToBeReceived);

                var errorContext = new ErrorContext(exception, headers, messageId, body, transportTransaction, processingFailures);

                ErrorHandleResult actionToTake;
                try
                {
                    actionToTake = await onError(errorContext, stopRequestedCancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    transaction.Rollback();

                    return;
                }
                catch (Exception ex)
                {
                    criticalErrorAction($"Failed to execute recoverability policy for message with native ID: `{messageContext.MessageId}`", ex, CancellationToken.None);
                    actionToTake = ErrorHandleResult.RetryRequired;
                }

                if (actionToTake == ErrorHandleResult.RetryRequired)
                {
                    transaction.Rollback();

                    return;
                }
            }

            await transaction.Commit(stopRequestedCancellationToken)
                .ConfigureAwait(false);
        }

        CancellationTokenSource stopRequestedCancellationTokenSource;
        SemaphoreSlim concurrencyLimiter;
        Task messagePumpTask;
        ConcurrentDictionary<string, int> retryCounts = new ConcurrentDictionary<string, int>();
        string messagePumpBasePath;
        string basePath;
        DelayedMessagePoller delayedMessagePoller;
        int maxConcurrency;
        string bodyDir;
        string pendingTransactionDir;
        string committedTransactionDir;
        string delayedDir;

        Action<string, Exception, CancellationToken> criticalErrorAction;
        readonly ReceiveSettings receiveSettings;
        readonly TransportTransactionMode transactionMode;


        static ILog log = LogManager.GetLogger<LearningTransportMessagePump>();
        OnMessage onMessage;
        OnError onError;

        public const string BodyFileSuffix = ".body.txt";
        public const string BodyDirName = ".bodies";
        public const string DelayedDirName = ".delayed";

        const string CommittedDirName = ".committed";
        const string PendingDirName = ".pending";
    }
}
