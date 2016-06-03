namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Features;
    using Routing;
    using Transports;


    class TimeoutRecPolicy : IRecoverabilityPolicy
    {
        public RecoveryAction Invoke(Exception exception, Dictionary<string, string> headers, int numberOfProcessingAttempts, Dictionary<string, string> metadata)
        {
            if (exception is MessageDeserializationException)
            {
                return new MoveToErrorQueue();
            }

            if (numberOfProcessingAttempts < 5)
            {
                return new ImmediateRetry();
            }

            return new MoveToErrorQueue();
        }
    }

    class MainRecoverabilityPolicy : IRecoverabilityPolicy
    {
        public MainRecoverabilityPolicy(SecondLevelRetryPolicy secondLevelRetryPolicy, int maxImmediateRetries)
        {
            this.secondLevelRetryPolicy = secondLevelRetryPolicy;
            this.maxImmediateRetries = maxImmediateRetries;
        }


        public RecoveryAction Invoke(Exception exception, Dictionary<string, string> headers, int numberOfProcessingAttempts, Dictionary<string, string> metadata)
        {
            if (exception is MessageDeserializationException)
            {
                return new MoveToErrorQueue();
            }
            var numberOfDelayedRetryAttempts = int.Parse(metadata[Headers.Retries]);

            var numberOfImmediateRetries = numberOfProcessingAttempts / numberOfDelayedRetryAttempts;

            if (ShouldDoImmediateRetry(numberOfImmediateRetries))
            {
                return new ImmediateRetry();
            }

            TimeSpan delay;

            if (secondLevelRetryPolicy.TryGetDelay(headers, exception, numberOfDelayedRetryAttempts, out delay))
            {
                return new DelayedRetry(delay, new Dictionary<string, string>
                {
                    {Headers.Retries, numberOfDelayedRetryAttempts.ToString()}
                });
            }


            return new MoveToErrorQueue();
        }

        bool ShouldDoImmediateRetry(int numberOfImmediateRetries)
        {
            return numberOfImmediateRetries < maxImmediateRetries;
        }

        SecondLevelRetryPolicy secondLevelRetryPolicy;
        int maxImmediateRetries;
    }

    class DelayedRetry : RecoveryAction
    {
        public TimeSpan Delay { get; }

        public DelayedRetry(TimeSpan delay, Dictionary<string, string> metadata)
        {
            Delay = delay;
        }
    }

    interface IRecoverabilityPolicy
    {
        RecoveryAction Invoke(Exception exception, Dictionary<string, string> headers, int numberOfProcessingAttempts, Dictionary<string, string> metadata);
    }

    class Recoverability : Feature
    {
        public Recoverability()
        {
            EnableByDefault();
            DependsOnOptionally<TimeoutManager>();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var inputQueueAddress = context.Settings.LocalAddress();

            var transportHasNativeDelayedDelivery = context.DoesTransportSupportConstraint<DelayedDeliveryConstraint>();
            var timeoutManagerEnabled = !IsTimeoutManagerDisabled(context);
            var timeoutManagerAddress = timeoutManagerEnabled
                ? context.Settings.Get<TimeoutManagerAddressConfiguration>().TransportAddress
                : string.Empty;

            var errorQueueAddress = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            var recoverabilityPolicy = new MainRecoverabilityPolicy(new DefaultSecondLevelRetryPolicy(2, TimeSpan.FromSeconds(10)), 2); 
            var recoveryExecutor = new RecoveryActionExecutor(recoverabilityPolicy, transportHasNativeDelayedDelivery, 
                timeoutManagerEnabled, inputQueueAddress, timeoutManagerAddress, errorQueueAddress);

            context.Container.RegisterSingleton(recoveryExecutor);
        }

        static bool IsTimeoutManagerDisabled(FeatureConfigurationContext context)
        {
            FeatureState timeoutMgrState;
            if (context.Settings.TryGet("NServiceBus.Features.TimeoutManager", out timeoutMgrState))
            {
                return timeoutMgrState == FeatureState.Deactivated || timeoutMgrState == FeatureState.Disabled;
            }
            return true;
        }
    }

    class RecoveryActionExecutor
    {
        readonly IRecoverabilityPolicy recoverabilityPolicy;
        readonly bool nativeDeferralsSupported;
        readonly bool timeoutManagerEnabled;
        readonly string inputQueueAddress;
        readonly string timeoutManagerQueueAddress;
        readonly string errorQueueAddress;

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

        public async Task<bool> RawInvoke(ErrorContext context, IDispatchMessages messageDispatcher)
        {
            var action = recoverabilityPolicy.Invoke(context.Exception, context.Headers, context.NumberOfProcessingAttempts, context.Metadata);

            if (action is ImmediateRetry)
            {
                return true;
            }

            var body = new byte[context.BodyStream.Length];
            context.BodyStream.Read(body, 0, body.Length);
            var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, body);

            if (action is DelayedRetry)
            {
                var delayWith = ((DelayedRetry) action).Delay;

                if (nativeDeferralsSupported)
                {
                    var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(inputQueueAddress), deliveryConstraints: new []{new DelayDeliveryWith(delayWith)});

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
                else
                {
                    //What do we do if no deferrals are possible?
                }

                return false;
            }

            if (action is MoveToErrorQueue)
            {
                outgoingMessage.SetExceptionHeaders(context.Exception, errorQueueAddress);

                outgoingMessage.Headers.Remove(Headers.Retries);
                outgoingMessage.Headers.Remove(Headers.FLRetries);

                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueueAddress));

                await messageDispatcher.Dispatch(new TransportOperations(transportOperation), context.Context).ConfigureAwait(false);

                return false;
            }


            //if(transport.HasNative)
            //   messageDispatcher.Dispatch(new DeliveryConstraint(delay), message,context);
            // else
            //  message.Headers["Delay"] = delay;
            //  messageDispatcher.Dispatch("timeoutsqueue",message,context);


            //if moveToError
            //   headers.Remove(Headers.Retries); //???
            // check that SC removes this header
            // var deferedRetry = await secondLevelRetries.Invoke(context.Exception, numberOfSecondLevelRetries, incomingMesage, context.Context).ConfigureAwait(false);
            //headers[Headers.Retries] = numberOfSecondLevelAttempts.ToString();
            //headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

            //await errorQueue.Invoke(errorQueueAddress, incomingMesage, context.Exception, messageDispatcher, context.Context).ConfigureAwait(false);

            //Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", messageId);
            //Logger.Info($"First Level Retry is going to retry message '{messageId}' because of an exception:", exception);

            //await context.RaiseNotification(new MessageToBeRetried(firstLevelRetries, TimeSpan.Zero, context.Message, ex)).ConfigureAwait(false);

            return false;
        }

        static bool IsDeferred(IExtendable context, out DateTime deliverAt)
        {
            deliverAt = DateTime.MinValue;
            DoNotDeliverBefore doNotDeliverBefore;
            DelayDeliveryWith delayDeliveryWith;
            if (context.Extensions.TryRemoveDeliveryConstraint(out doNotDeliverBefore))
            {
                deliverAt = doNotDeliverBefore.At;
                return true;
            }
            if (context.Extensions.TryRemoveDeliveryConstraint(out delayDeliveryWith))
            {
                deliverAt = DateTime.UtcNow + delayDeliveryWith.Delay;
                return true;
            }
            return false;
        }


        public int GetSecondLevelRetryAttemptFromHeaders()
        {
            throw new NotImplementedException();
        }
    }
    abstract class RecoveryAction
    {

    }

    class ImmediateRetry : RecoveryAction
    {
    }

    class MoveToErrorQueue : RecoveryAction
    { }
}