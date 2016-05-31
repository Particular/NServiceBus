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

    class ReceiveWithNoTransaction : ReceiveStrategy
    {
        public override async Task ReceiveMessage(MessageQueue inputQueue, MessageQueue errorQueue, CancellationTokenSource cancellationTokenSource, Func<PushContext, Task> onMessage, Func<ErrorContext, Task> onError)
        {
            var message = inputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.None);

            Dictionary<string, string> headers;

            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' is corrupt and will be moved to '{errorQueue.QueueName}'";
                Logger.Error(error, ex);

                errorQueue.Send(message, errorQueue.Transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

                return;
            }

            try
            {
                using (var bodyStream = message.BodyStream)
                {
                    var pushContext = new PushContext(message.Id, headers, bodyStream, new TransportTransaction(), cancellationTokenSource, new ContextBag());

                    await onMessage(pushContext).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await onError(new ErrorContext(ex, true)).ConfigureAwait(false);
            }
        }

        static ILog Logger = LogManager.GetLogger<ReceiveWithNoTransaction>();
    }
}