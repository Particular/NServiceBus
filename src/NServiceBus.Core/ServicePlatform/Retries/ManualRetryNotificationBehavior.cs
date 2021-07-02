namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    class ManualRetryNotificationBehavior : IForkConnector<ITransportReceiveContext, ITransportReceiveContext, IRoutingContext>
    {
        internal const string RetryUniqueMessageIdHeaderKey = "ServiceControl.Retry.UniqueMessageId";
        internal const string RetryConfirmationQueueHeaderKey = "ServiceControl.Retry.AcknowledgementQueue";

        public async Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
        {
            var useRetryAcknowledgement = IsRetriedMessage(context, out var id, out var acknowledgementQueue);

            if (useRetryAcknowledgement)
            {
                // notify the ServiceControl audit instance that the retry has already been acknowledged by the endpoint
                context.Extensions.Set(new MarkAsAcknowledgedBehavior.State());
            }

            await next(context).ConfigureAwait(false);

            if (useRetryAcknowledgement)
            {
                await ConfirmSuccessfulRetry().ConfigureAwait(false);
            }

            async Task ConfirmSuccessfulRetry()
            {
                var messageToDispatch = new OutgoingMessage(
                    CombGuid.Generate().ToString(),
                    new Dictionary<string, string>
                    {
                        { "ServiceControl.Retry.Successful", DateTimeOffset.UtcNow.ToString("O") },
                        { RetryUniqueMessageIdHeaderKey, id },
                        { Headers.ControlMessageHeader, bool.TrueString }
                    },
                    new byte[0]);
                var routingContext = new RoutingContext(messageToDispatch, new UnicastRoutingStrategy(acknowledgementQueue), context);
                await this.Fork(routingContext).ConfigureAwait(false);
            }
        }

        static bool IsRetriedMessage(ITransportReceiveContext context, out string retryUniqueMessageId, out string retryAcknowledgementQueue)
        {
            // check if the message is coming from a manual retry attempt
            if (context.Message.Headers.TryGetValue(RetryUniqueMessageIdHeaderKey, out var uniqueMessageId) &&
                // The SC version that supports the confirmation message also started to add the SC version header
                context.Message.Headers.TryGetValue(RetryConfirmationQueueHeaderKey, out var acknowledgementQueue))
            {
                retryUniqueMessageId = uniqueMessageId;
                retryAcknowledgementQueue = acknowledgementQueue;
                return true;
            }

            retryUniqueMessageId = null;
            retryAcknowledgementQueue = null;
            return false;
        }
    }
}