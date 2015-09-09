namespace NServiceBus.Transports.Msmq
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;

    class ReceiveWithNativeTransaction : ReceiveStrategy
    {
        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, Func<PushContext, Task> onMessage)
        {
            using (var msmqTransaction = new MessageQueueTransaction())
            {
                try
                {
                    msmqTransaction.Begin();

                    var message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), msmqTransaction);

                    Dictionary<string, string> headers;

                    try
                    {
                        headers = MsmqUtilities.ExtractHeaders(message);
                    }
                    catch (Exception ex)
                    {
                        var error = string.Format("Message '{0}' is corrupt and will be moved to '{1}'", message.Id, errorQueue.QueueName);
                        Logger.Error(error, ex);

                        errorQueue.Send(message, msmqTransaction);

                        msmqTransaction.Commit();
                        return;
                    }

                    using (var bodyStream = message.BodyStream)
                    {
                        var incomingMessage = new IncomingMessage(message.Id, headers, bodyStream);
                        var context = new ContextBag();

                        context.Set(msmqTransaction);

                        await onMessage(new PushContext(incomingMessage, context)).ConfigureAwait(false);
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