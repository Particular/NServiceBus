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
        public override async Task ReceiveMessage(CancellationTokenSource cancellationTokenSource)
        {
            var message = InputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.None);

            Dictionary<string, string> headers;

            try
            {
                headers = MsmqUtilities.ExtractHeaders(message);
            }
            catch (Exception ex)
            {
                var error = $"Message '{message.Id}' is corrupt and will be moved to '{ErrorQueue.QueueName}'";
                Logger.Error(error, ex);

                ErrorQueue.Send(message, ErrorQueue.Transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

                return;
            }

            try
            {
                using (var bodyStream = message.BodyStream)
                {
                    var pushContext = new MessageContext(message.Id, headers, bodyStream, new TransportTransaction(), cancellationTokenSource, new ContextBag());

                    await OnMessage(pushContext).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                message.BodyStream.Position = 0;

                await OnError(new ErrorContext()).ConfigureAwait(false);
            }
        }

        static ILog Logger = LogManager.GetLogger<ReceiveWithNoTransaction>();
    }
}