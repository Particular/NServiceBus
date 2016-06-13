namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Logging;
    using Routing;
    using Transports;

    class RecoveryActionExecutor
    {
        public RecoveryActionExecutor(IRecoverabilityPolicy recoverabilityPolicy, bool nativeDeferralsSupported, bool timeoutManagerEnabled,
            string inputQueueAddress, string timeoutManagerQueueAddress, string errorQueueAddress)
        {
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.nativeDeferralsSupported = nativeDeferralsSupported;
            this.timeoutManagerEnabled = timeoutManagerEnabled;
            this.inputQueueAddress = inputQueueAddress;
            this.timeoutManagerQueueAddress = timeoutManagerQueueAddress;
            this.errorQueueAddress = errorQueueAddress;
        }

        public async Task<bool> RawInvoke(ErrorContext context, IDispatchMessages messageDispatcher, IEventAggregator eventAggregator)
        {
            var recoverabilityMetadata = ExtractMetadata(context.Headers);

            var action = recoverabilityPolicy.Invoke(context.Exception, context.Headers, context.NumberOfProcessingAttempts, recoverabilityMetadata);

            if (action is ImmediateRetry)
            {
                Logger.Info($"First Level Retry is going to retry message '{context.MessageId}' because of an exception:", context.Exception);
                return true;
            }

            var body = new byte[context.BodyStream.Length];

            await context.BodyStream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);

            var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, body);

            if (action is MoveToErrorQueue)
            {
                outgoingMessage.SetExceptionHeaders(context.Exception, errorQueueAddress);

                outgoingMessage.Headers.Remove(Headers.Retries);
                outgoingMessage.Headers.Remove(Headers.FLRetries);

                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress));

                await messageDispatcher.Dispatch(new TransportOperations(transportOperation), context.Context).ConfigureAwait(false);

                Logger.Error($"Moving message '{outgoingMessage.MessageId}' to the error queue because processing failed due to an exception:", context.Exception);

                var incomingMessage = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);
                await eventAggregator.Raise(new MessageFaulted(incomingMessage, context.Exception)).ConfigureAwait(false);

                return false;
            }

            var delayedRetry = action as DelayedRetry;

            if (delayedRetry == null)
            {
                throw new InvalidOperationException($"{action.GetType().Name} is not a supported recovery action");
            }

            var delayWith = delayedRetry.Delay;

            MergeMetadata(outgoingMessage.Headers, recoverabilityMetadata);

            if (nativeDeferralsSupported)
            {
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(inputQueueAddress), deliveryConstraints: new[]
                {
                    new DelayDeliveryWith(delayWith)
                });

                await messageDispatcher.Dispatch(new TransportOperations(transportOperation), context.Context).ConfigureAwait(false);
            }
            else if (timeoutManagerEnabled)
            {
                var deliverAt = DateTime.UtcNow + delayWith;

                outgoingMessage.Headers[TimeoutManagerHeaders.RouteExpiredTimeoutTo] = inputQueueAddress;
                outgoingMessage.Headers[TimeoutManagerHeaders.Expire] = DateTimeExtensions.ToWireFormattedString(deliverAt);

                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(timeoutManagerQueueAddress));

                await messageDispatcher.Dispatch(new TransportOperations(transportOperation), context.Context).ConfigureAwait(false);
            }

            Logger.Warn($"Second Level Retry will reschedule message '{outgoingMessage.MessageId}' after a delay of {delayWith} because of an exception:", context.Exception);

            //todo: figure out how to do this
            //await context.RaiseNotification(new MessageToBeRetried(firstLevelRetries, TimeSpan.Zero, context.Message, ex)).ConfigureAwait(false);

            return false;
        }


        static Dictionary<string, string> ExtractMetadata(Dictionary<string, string> headers)
        {
            return headers.Where(kv => kv.Key.StartsWith("$.Recoverability") || kv.Key == Headers.Retries).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        static void MergeMetadata(Dictionary<string, string> headers, Dictionary<string, string> metadata)
        {
            foreach (var metadataKeyValue in metadata)
            {
                var headersKey = metadataKeyValue.Key == Headers.Retries
                    ? Headers.Retries
                    : $"$.Recoverability.{metadataKeyValue.Key}";

                if (headers.ContainsKey(headersKey))
                {
                    headers.Add(headersKey, metadataKeyValue.Value);
                }
                else
                {
                    headers[headersKey] = metadataKeyValue.Value;
                }
            }
        }

        IRecoverabilityPolicy recoverabilityPolicy;
        bool nativeDeferralsSupported;
        bool timeoutManagerEnabled;
        string inputQueueAddress;
        string timeoutManagerQueueAddress;
        string errorQueueAddress;

        static ILog Logger = LogManager.GetLogger<RecoveryActionExecutor>();
    }
}