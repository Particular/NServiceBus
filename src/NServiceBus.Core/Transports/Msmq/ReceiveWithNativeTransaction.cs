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
                    try
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

                        var transportTransaction = new TransportTransaction();

                        transportTransaction.Set(msmqTransaction);

                        MsmqFailureInfoStorage.ProcessingFailureInfo failureInfo;

                        var shouldTryProcessMessage = true;

                        if (failureInfoStorage.TryGetFailureInfoForMessage(message.Id, out failureInfo))
                        {
                            var shouldRetryImmediately = await HandleError(message, headers, failureInfo.Exception).ConfigureAwait(false);

                            shouldTryProcessMessage = shouldRetryImmediately;
                        }

                        if (shouldTryProcessMessage)
                        {
                            var shouldAbortMessageProcessing = await TryProcessMessage(message, headers, transportTransaction).ConfigureAwait(false);

                            if (shouldAbortMessageProcessing)
                            {
                                msmqTransaction.Abort();
                                return;
                            }
                        }

                        failureInfoStorage.ClearFailureInfoForMessage(message.Id);

                        msmqTransaction.Commit();
                    }
                    catch (Exception)
                    {
                        msmqTransaction.Abort();

                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                if (message == null)
                {
                    throw;
                }

                failureInfoStorage.RecordFailureInfoForMessage(message.Id, exception);
            }
        }

        MsmqFailureInfoStorage failureInfoStorage;
    }
}