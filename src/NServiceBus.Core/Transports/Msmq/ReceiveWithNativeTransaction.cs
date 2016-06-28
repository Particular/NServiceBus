namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Transports;

    class ReceiveWithNativeTransaction : ReceiveStrategy
    {
        public ReceiveWithNativeTransaction(MsmqFailureInfoStorage failureInfoStorage)
        {
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task ReceiveMessage(CancellationTokenSource cancellationTokenSource)
        {
            Message message = null;

            try
            {
                using (var msmqTransaction = new MessageQueueTransaction())
                {
                    try
                    {
                        msmqTransaction.Begin();

                        // ReSharper disable once AccessToDisposedClosure
                        if (!TryReceive(msmqTransaction, out message))
                        {
                            return;
                        }

                        Dictionary<string, string> headers;

                        if (!TryExtractHeaders(message, MessageQueueTransactionType.Automatic, out headers))
                        {
                            msmqTransaction.Commit();
                            return;
                        }

                        var transportTransaction = new TransportTransaction();

                        transportTransaction.Set(msmqTransaction);

                        var shouldAbort = await TryProcessMessage(message, headers, transportTransaction).ConfigureAwait(false);

                        if (shouldAbort)
                        {
                            msmqTransaction.Abort();
                            return;
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

        static ILog Logger = LogManager.GetLogger<ReceiveWithNativeTransaction>();
    }
}