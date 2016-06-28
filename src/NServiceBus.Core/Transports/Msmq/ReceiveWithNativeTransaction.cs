namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
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
                        if (!TryReceive(queue => InputQueue.Receive(TimeSpan.FromMilliseconds(10), msmqTransaction), out message))
                        {
                            return;
                        }

                        Dictionary<string, string> headers;

                        try
                        {
                            headers = MsmqUtilities.ExtractHeaders(message);
                        }
                        catch (Exception ex)
                        {
                            var error = $"Message '{message.Id}' is corrupt and will be moved to '{ErrorQueue.QueueName}'";
                            Logger.Error(error, ex);

                            ErrorQueue.Send(message, msmqTransaction);

                            msmqTransaction.Commit();
                            return;
                        }

                        using (var bodyStream = message.BodyStream)
                        {
                            var nativeMsmqTransaction = new TransportTransaction();
                            nativeMsmqTransaction.Set(msmqTransaction);

                            var pushContext = new MessageContext(message.Id, headers, bodyStream, nativeMsmqTransaction, cancellationTokenSource, new ContextBag());

                            await OnMessage(pushContext).ConfigureAwait(false);
                        }

                        if (cancellationTokenSource.Token.IsCancellationRequested)
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