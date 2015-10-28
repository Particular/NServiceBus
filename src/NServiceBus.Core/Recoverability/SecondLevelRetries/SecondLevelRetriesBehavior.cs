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
            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;
            var exception = slrStatusStorage.GetExceptionForRetry(uniqueMessageId);

            if (exception != null)
            {
                var message = context.Message;
                var currentRetry = GetNumberOfRetries(message.Headers) + 1;

                TimeSpan delay;
                if (ShouldPerformSlr(message, exception, currentRetry, out delay))
                {
                    await PerformSlr(context, message, currentRetry, delay, exception, uniqueMessageId);
                    return;
                }

                TriggerNextFailureProcessingStage(message, exception);
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
                slrStatusStorage.MarkForRetry(uniqueMessageId, ex);

                throw new MessageProcessingAbortedException();
            }
        }

        bool ShouldPerformSlr(IncomingMessage message, Exception exception, int currentRetry, out TimeSpan delay)
        {
            return retryPolicy.TryGetDelay(message, exception, currentRetry, out delay);
        }

        async Task PerformSlr(TransportReceiveContext context, IncomingMessage message, int currentRetry, TimeSpan delay, Exception exception, string uniqueMessageId)
        {
            slrStatusStorage.MarkAsPickedUpForRetry(uniqueMessageId);

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

        static void TriggerNextFailureProcessingStage(IncomingMessage message, Exception exception)
        {
            message.Headers.Remove(Headers.Retries);
            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);

            throw exception;
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
        BusNotifications notifications;
        string localAddress;
        readonly SlrStatusStorage slrStatusStorage;

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