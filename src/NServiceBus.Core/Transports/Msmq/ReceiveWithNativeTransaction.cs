namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Messaging;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Transports;

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
                        var error = $"Message '{message.Id}' is corrupt and will be moved to '{errorQueue.QueueName}'";
                        Logger.Error(error, ex);

                        errorQueue.Send(message, msmqTransaction);

                        msmqTransaction.Commit();
                        return;
                    }

                    using (var bodyStream = message.BodyStream)
                    {
                        var context = new ContextBag();

                        context.Set(msmqTransaction);

                        var pushContext = new PushContext(message.Id, headers, bodyStream, context);

                        await onMessage(pushContext).ConfigureAwait(false);
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