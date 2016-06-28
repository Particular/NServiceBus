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
            Message message;

            if (!TryReceive(queue => InputQueue.Receive(TimeSpan.FromMilliseconds(10), MessageQueueTransactionType.None), out message))
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

                ErrorQueue.Send(message, ErrorQueue.Transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);

                return;
            }

            try
            {
                await TryProcessMessage(message, headers, new TransportTransaction()).ConfigureAwait(false);
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