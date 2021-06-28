namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    class ManualRetryNotificationBehavior : IForkConnector<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext, IRoutingContext>
    {
        const string RetryUniqueMessageIdHeader = "ServiceControl.Retry.UniqueMessageId";

        readonly string errorQueue;

        public ManualRetryNotificationBehavior(string errorQueue)
        {
            this.errorQueue = errorQueue;
        }

        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            await next(context).ConfigureAwait(false);

            if (IsRetriedMessage(out var id))
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
                        { RetryUniqueMessageIdHeader, id },
                        { Headers.ControlMessageHeader, bool.TrueString }
                    },
                    new byte[0]);
                var routingContext = new RoutingContext(messageToDispatch, new UnicastRoutingStrategy(errorQueue), context);
                await this.Fork(routingContext).ConfigureAwait(false);
            }

            bool IsRetriedMessage(out string retryUniqueMessageId)
            {
                // check if the message is coming from a manual retry attempt
                if (context.Headers.TryGetValue(RetryUniqueMessageIdHeader, out var uniqueMessageId) &&
                    // The SC version that supports the confirmation message also started to add the SC version header
                    context.Headers.ContainsKey("ServiceControl.Version"))
                {
                    retryUniqueMessageId = uniqueMessageId;
                    return true;
                }

                retryUniqueMessageId = null;
                return false;
            }
        }
    }
}