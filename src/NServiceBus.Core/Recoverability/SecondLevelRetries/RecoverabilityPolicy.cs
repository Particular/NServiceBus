namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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

    class RecoveryActionExecutor
    {
        readonly IRecoverabilityPolicy recoverabilityPolicy;

        public RecoveryActionExecutor(IRecoverabilityPolicy recoverabilityPolicy)
        {
            this.recoverabilityPolicy = recoverabilityPolicy;
        }

        public async Task<bool> RawInvoke(ErrorContext context, IDispatchMessages messageDispatcher, string errorQueueAddress)
        {
            var action = recoverabilityPolicy.Invoke(context.Exception, context.Headers, context.NumberOfProcessingAttempts, context.Metadata);


            var incomingMesage = new IncomingMessage(context.MessageId, context.Headers, context.BodyStream);

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