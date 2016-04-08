namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Transports;

    class SecondLevelRetriesBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress, FailureInfoStorage failureInfoStorage)
        {
            this.retryPolicy = retryPolicy;
            this.localAddress = localAddress;
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
        {
            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(context.Message.MessageId);

            if (failureInfo.DeferForSecondLevelRetry)
            {
                await DeferMessageForSecondLevelRetry(context, fork, context.Message, failureInfo).ConfigureAwait(false);

                return;
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                context.Message.Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                failureInfoStorage.MarkForDeferralForSecondLevelRetry(context.Message.MessageId, ExceptionDispatchInfo.Capture(ex));

                context.AbortReceiveOperation();
            }
        }

        async Task DeferMessageForSecondLevelRetry(ITransportReceiveContext context, Func<IRoutingContext, Task> fork, IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;

            message.Headers[Headers.FLRetries] = failureInfo.FLRetries.ToString();

            TimeSpan delay;

            if (retryPolicy.TryGetDelay(message, failureInfo.Exception, currentRetry, out delay))
            {
                message.RevertToOriginalBodyIfNeeded();
                var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

                messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                messageToRetry.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

                var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

                context.Extensions.Set(new List<DeliveryConstraint>
                {
                    new DelayDeliveryWith(delay)
                });

                Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", failureInfo.Exception);

                await fork(dispatchContext).ConfigureAwait(false);

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await context.RaiseNotification(new MessageToBeRetried(currentRetry, delay, context.Message, failureInfo.Exception)).ConfigureAwait(false);

                return;
            }

            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);

            failureInfo.ExceptionDispatchInfo.Throw();
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

        FailureInfoStorage failureInfoStorage;
        string localAddress;
        SecondLevelRetryPolicy retryPolicy;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

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