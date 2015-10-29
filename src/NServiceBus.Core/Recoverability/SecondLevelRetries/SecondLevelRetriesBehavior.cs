namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Recoverability.SecondLevelRetries;
    using Routing;
    using TransportDispatch;
    using Transports;

    class SecondLevelRetriesBehavior : Behavior<TransportReceiveContext>
    {
        public SecondLevelRetriesBehavior(IPipelineBase<RoutingContext> dispatchPipeline, SecondLevelRetryPolicy retryPolicy, BusNotifications notifications, string localAddress, SlrStatusStorage slrStatusStorage)
        {
            this.dispatchPipeline = dispatchPipeline;
            this.retryPolicy = retryPolicy;
            this.notifications = notifications;
            this.localAddress = localAddress;
            this.slrStatusStorage = slrStatusStorage;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            Exception exception;
            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;

            if (slrStatusStorage.TryGetException(uniqueMessageId, out exception))
            {
                slrStatusStorage.ClearException(uniqueMessageId);

                TimeSpan delay;
                int currentRetry;

                if (ShouldPerformSlr(context.Message, exception, out delay, out currentRetry))
                {
                    await QueueForDelayedDelivery(context, currentRetry, delay, exception);

                    return;
                }

                context.Message.Headers.Remove(Headers.Retries);

                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", context.Message.MessageId);
                throw exception;
            }

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
            catch (Exception ex)
            {
                //Mark for processing on next receive
                slrStatusStorage.AddException(uniqueMessageId, ex);

                throw new MessageProcessingAbortedException();
            }
        }

        bool ShouldPerformSlr(IncomingMessage message, Exception exception,  out TimeSpan delay, out int currentRetry)
        {
            currentRetry = GetNumberOfRetries(message.Headers) + 1;

            return retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);
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

        async Task QueueForDelayedDelivery(TransportReceiveContext context, int currentRetry, TimeSpan delay, Exception exception)
        {
            var message = context.Message;

            message.RevertToOriginalBodyIfNeeded();

            var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

            messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
            messageToRetry.Headers[RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var dispatchContext = new RoutingContext(messageToRetry, new UnicastRoutingStrategy(localAddress), context);

            context.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(delay)
            });

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", exception);

            await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

            notifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(currentRetry, message, exception);
        }

        IPipelineBase<RoutingContext> dispatchPipeline;
        SecondLevelRetryPolicy retryPolicy;
        BusNotifications notifications;
        string localAddress;
        SlrStatusStorage slrStatusStorage;

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