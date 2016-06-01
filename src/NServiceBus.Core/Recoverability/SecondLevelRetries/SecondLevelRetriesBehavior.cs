namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Logging;
    using Pipeline;
    using Routing;
    using Transports;

    class SecondLevelRetriesBehavior
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress, FailureInfoStorage failureInfoStorage, IDispatchMessages dispatcher)
        {
            this.retryPolicy = retryPolicy;
            this.localAddress = localAddress;
            this.failureInfoStorage = failureInfoStorage;
            this.dispatcher = dispatcher;
        }

        public async Task<bool> Invoke(Exception exception, int numberOfSecondLevelAttempts, IncomingMessage failedMessage, ContextBag context)
        {
            if (exception is MessageDeserializationException)
            {
                failedMessage.Headers.Remove(Headers.Retries); //???
                return false;
            }

            TimeSpan delay;

            if (!retryPolicy.TryGetDelay(failedMessage, exception, numberOfSecondLevelAttempts, out delay))
            {
                return false;
            }

            failedMessage.Headers[Headers.Retries] = numberOfSecondLevelAttempts.ToString();
            failedMessage.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            var operation = new TransportOperation(new OutgoingMessage(failedMessage.MessageId, failedMessage.Headers, failedMessage.Body), new UnicastAddressTag(localAddress));
            
            context.Extensions.Set(new List<DeliveryConstraint>
            {
                new DelayDeliveryWith(delay)
            });

            Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", failureInfo.Exception);

            await dispatcher.Dispatch(new TransportOperations(operation), context).ContinueWith(false);
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

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await fork(dispatchContext).ConfigureAwait(false);

                //await context.RaiseNotification(new MessageToBeRetried(currentRetry, delay, context.Message, failureInfo.Exception)).ConfigureAwait(false);

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
        readonly IDispatchMessages dispatcher;
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