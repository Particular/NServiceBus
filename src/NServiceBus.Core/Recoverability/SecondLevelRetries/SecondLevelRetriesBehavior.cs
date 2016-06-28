namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;

    class SecondLevelRetriesBehavior : Behavior<ITransportReceiveContext>
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, FailureInfoStorage failureInfoStorage, DelayedRetryExecutor delayedRetryExecutor)
        {
            this.retryPolicy = retryPolicy;
            this.failureInfoStorage = failureInfoStorage;
            this.delayedRetryExecutor = delayedRetryExecutor;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(context.Message.MessageId);

            if (failureInfo.DeferForSecondLevelRetry)
            {
                await DeferMessageForSecondLevelRetry(context, context.Message, failureInfo).ConfigureAwait(false);

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

        async Task DeferMessageForSecondLevelRetry(ITransportReceiveContext context, IncomingMessage message, ProcessingFailureInfo failureInfo)
        {
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;

            message.Headers[Headers.FLRetries] = failureInfo.FLRetries.ToString();

            TimeSpan delay;

            if (retryPolicy.TryGetDelay(new SecondLevelRetryContext
            {
                Message = message,
                Exception = failureInfo.Exception,
                SecondLevelRetryAttempt = currentRetry
            }, out delay))
            {
                Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", failureInfo.Exception);

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await delayedRetryExecutor.Retry(context.Message, delay, context.Extensions).ConfigureAwait(false);

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
        DelayedRetryExecutor delayedRetryExecutor;
        SecondLevelRetryPolicy retryPolicy;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();
    }
}