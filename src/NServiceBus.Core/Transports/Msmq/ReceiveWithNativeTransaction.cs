namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Transports;

    class ReceiveWithNativeTransaction : ReceiveStrategy
    {
        public ReceiveWithNativeTransaction(MsmqFailureInfoStorage failureInfoStorage)
        {
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage()
        {
            Message message = null;

            try
            {
                using (var msmqTransaction = new MessageQueueTransaction())
                {
                    msmqTransaction.Begin();

                    if (!TryReceive(msmqTransaction, out message))
                    {
                        return;
                    }

                    Dictionary<string, string> headers;

                    if (!TryExtractHeaders(message, out headers))
                    {
                        MovePoisonMessageToErrorQueue(message, msmqTransaction);

                        msmqTransaction.Commit();
                        return;
                    }

                    var shouldCommit = await ProcessMessage(msmqTransaction, message, headers).ConfigureAwait(false);

                    if (shouldCommit)
                    {
                        msmqTransaction.Commit();
                    }
                    else
                    {
                        msmqTransaction.Abort();
                    }
                }
            }
            // We'll only get here if Commit/Abort/Dispose throws which should be rare.
            // Note: If that happens the attempts counter will be inconsistent since the message might be picked up again before we can register the failure in the LRU cache.
            catch (Exception exception)
            {
                if (message == null)
                {
                    throw;
                }

                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
            }
        }

        async Task<bool> ProcessMessage(MessageQueueTransaction msmqTransaction, Message message, Dictionary<string, string> headers)
        {
            var transportTransaction = new TransportTransaction();

            transportTransaction.Set(msmqTransaction);

            MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

            var shouldTryProcessMessage = true;

            if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
            {
                var shouldRetryImmediately = await HandleError(message, headers, failureInfo.Exception, failureInfo.NumberOfProcessingAttempts, transportTransaction).ConfigureAwait(false);

                shouldTryProcessMessage = shouldRetryImmediately;
            }

            if (shouldTryProcessMessage)
            {
                try
                {
                    using (var bodyStream = message.BodyStream)
                    {
                        var shouldAbortMessageProcessing = await TryProcessMessage(message, headers, bodyStream, transportTransaction).ConfigureAwait(false);

                        if (shouldAbortMessageProcessing)
                        {
                            return false;
                        }
                    }
                }
                catch (Exception exception)
                {
                    failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);

                    return false;
                }
            }

            failureInfoStorage.ClearFailureInfoForMessage(message.Id);

            return true;
        }

        MsmqFailureInfoStorage failureInfoStorage;
    }
}