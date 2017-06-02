namespace NServiceBus
{
    using NServiceBus.Logging;
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Transport;

    class ReceiveOnlyNativeTransactionStrategy : ReceiveStrategy
    {
        public ReceiveOnlyNativeTransactionStrategy(MsmqFailureInfoStorage failureInfoStorage)
        {
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage()
        {
            Message message = null;

            try
            {
                log.Info("ReceiveMessage - using MSMQ transaction");
                using (var msmqTransaction = new MessageQueueTransaction())
                {
                    log.Info("ReceiveMessage - msmqTransaction.Begin()");
                    msmqTransaction.Begin();

                    log.Info("ReceiveMessage - TryReceive");
                    if (!TryReceive(msmqTransaction, out message))
                    {
                        return;
                    }

                    Dictionary<string, string> headers;

                    log.Info("ReceiveMessage - TryExtractHeaders");
                    if (!TryExtractHeaders(message, out headers))
                    {
                        MovePoisonMessageToErrorQueue(message, IsQueuesTransactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

                        msmqTransaction.Commit();
                        return;
                    }

                    log.Info("ReceiveMessage - Call ProcessMessage");
                    var shouldCommit = await ProcessMessage(message, headers).ConfigureAwait(false);

                    if (shouldCommit)
                    {
                        log.Info("ReceiveMessage - committing MSMQ transaction");
                        msmqTransaction.Commit();
                        failureInfoStorage.ClearFailureInfoForMessage(message.Id);
                    }
                    else
                    {
                        log.Info("ReceiveMessage - aborting MSMQ transaction");
                        msmqTransaction.Abort();
                    }
                    log.Info("ReceiveMessage - disposing MsmqTransaction");
                }
                log.Info("ReceiveMessage - MsmqTransaction disposed");
            }
            // We'll only get here if Commit/Abort/Dispose throws which should be rare.
            // Note: If that happens the attempts counter will be inconsistent since the message might be picked up again before we can register the failure in the LRU cache.
            catch (Exception exception)
            {
                log.Info($"ReceiveMessage - Caught {exception.GetType().FullName}: {exception.Message}");
                if (message == null)
                {
                    log.Info("ReceiveMessage - No message, throwing");
                    throw;
                }

                log.Info($"ReceiveMessage - Exception Handler - Recording failure for MSMQ MsgId {message.Id}");
                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
                log.Info($"ReceiveMessage - Exception Handler - Failure recording complete for MSMQ MsgId {message.Id}");
            }
        }

        async Task<bool> ProcessMessage(Message message, Dictionary<string, string> headers)
        {
            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
            {
                log.Info($"ProcessMessage - Have failure info for MsmqMsgId {message.Id}");
                var errorHandleResult = await HandleError(message, headers, failureInfo.Exception, transportTransaction, failureInfo.NumberOfProcessingAttempts).ConfigureAwait(false);
                log.Info($"ProcessMessage - HandleError complete, Result={errorHandleResult}");

                if (errorHandleResult == ErrorHandleResult.Handled)
                {
                    return true;
                }
            }

            try
            {
                using (var bodyStream = message.BodyStream)
                {
                    log.Info("ProcessMessage - calling ReceiveStrategy.TryProcessMessage()");
                    var shouldAbortMessageProcessing = await TryProcessMessage(message.Id, headers, bodyStream, transportTransaction).ConfigureAwait(false);
                    log.Info("ProcessMessage - ReceiveStrategy.TryProcessMessage() complete");

                    if (shouldAbortMessageProcessing)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception exception)
            {
                log.Info($"ProcessMessage - Caught {exception.GetType().FullName}: {exception.Message} for MsmqMsgId {message.Id}");
                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
                log.Info($"ProcessMessage - Catch - recorded failure");

                return false;
            }
        }

        MsmqFailureInfoStorage failureInfoStorage;
        static TransportTransaction transportTransaction = new TransportTransaction();
        static readonly ILog log = LogManager.GetLogger<ReceiveOnlyNativeTransactionStrategy>();
    }
}