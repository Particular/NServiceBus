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
        public override async Task ReceiveMessage(CancellationTokenSource cancellationTokenSource)
        {
            using (var msmqTransaction = new MessageQueueTransaction())
            {
                try
                {
                    msmqTransaction.Begin();

                    Message message;

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

                    msmqTransaction.Commit();
                }
                catch (Exception)
                {
                    msmqTransaction.Abort();

                    throw;
                }
            }
        }

        static ILog Logger = LogManager.GetLogger<ReceiveWithNativeTransaction>();
    }
}