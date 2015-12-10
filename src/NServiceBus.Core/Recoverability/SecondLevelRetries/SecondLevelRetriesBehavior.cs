namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using NServiceBus.Faults;
    using Pipeline;
    using Pipeline.Contexts;
    using Recoverability.SecondLevelRetries;
    using Routing;
    using TransportDispatch;
    using Transports;

    class SecondLevelRetriesBehavior : Behavior<TransportReceiveContext>
    {
        public SecondLevelRetriesBehavior(IPipelineBase<RoutingContext> dispatchPipeline, SecondLevelRetryPolicy retryPolicy, IEnumerable<Action<SecondLevelRetry>> secondLevelRetryActions, string localAddress)
        {
            this.dispatchPipeline = dispatchPipeline;
            this.retryPolicy = retryPolicy;
            this.secondLevelRetryActions = secondLevelRetryActions.ToList();
            this.localAddress = localAddress;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageProcessingAbortedException)
            {
                throw; // flr asked to abort
            }
            catch (MessageDeserializationException)
            {
                context.Message.Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception exception)
            {
                var message = context.Message;
                var currentRetry = GetNumberOfRetries(message.Headers) + 1;

                TimeSpan delay;

                if (retryPolicy.TryGetDelay(message, exception, currentRetry, out delay))
                {
                    message.RevertToOriginalBodyIfNeeded();
                    var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

                    messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                    messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);


                    var dispatchContext = new RoutingContextImpl(messageToRetry, new UnicastRoutingStrategy(localAddress), context);

                    context.Extensions.Set(new List<DeliveryConstraint>
                    {
                        new DelayDeliveryWith(delay)
                    });

                    Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", exception);

                    await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

                    InvokeNotification(message, exception, currentRetry);
                    return;
                }

                message.Headers.Remove(Headers.Retries);
                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);
                throw;
            }

        }

        void InvokeNotification(IncomingMessage message, Exception exception, int currentRetry)
        {
            var secondLevelRetry = new SecondLevelRetry(message.Headers, message.Body, exception, currentRetry);
            foreach (var secondLevelRetryAction in secondLevelRetryActions)
            {
                secondLevelRetryAction(secondLevelRetry);
            }
        }

        static int GetNumberOfRetries(Dictionary<string, string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }


        IPipelineBase<RoutingContext> dispatchPipeline;
        SecondLevelRetryPolicy retryPolicy;
        List<Action<SecondLevelRetry>> secondLevelRetryActions;
        string localAddress;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

        public const string RetriesTimestamp = "NServiceBus.Retries.Timestamp";

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SecondLevelRetries", typeof(SecondLevelRetriesBehavior), "Performs second level retries")
            {
                InsertBeforeIfExists("FirstLevelRetries");
            }
        }

    }
}