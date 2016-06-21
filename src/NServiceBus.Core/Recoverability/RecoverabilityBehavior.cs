namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class RecoverabilityBehavior : Behavior<ITransportReceiveContext>
    {
        public RecoverabilityBehavior(FirstLevelRetriesHandler flrHandler, SecondLevelRetriesHandler slrHandler, MoveFaultsToErrorQueueHandler errorHandler,
            bool flrEnabled, bool slrEnabled, bool runningWithTransactions)
        {
            this.flrHandler = flrHandler;
            this.slrHandler = slrHandler;
            this.errorHandler = errorHandler;

            this.flrEnabled = flrEnabled;
            this.slrEnabled = slrEnabled;
            this.runningWithTransactions = runningWithTransactions;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            if (await errorHandler.HandleIfPreviouslyFailed(context).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                await ExecuteWithRetries(context, next).ConfigureAwait(true);
            }
            catch (Exception ex)
            {

                if (runningWithTransactions)
                {
                    errorHandler.MarkForFutureHandling(context, ex);
                }
                else
                {
                    await errorHandler.MoveMessageToErrorQueue(context, ex).ConfigureAwait(false);
                }
            }
        }

        async Task ExecuteWithRetries(ITransportReceiveContext context, Func<Task> next)
        {
            if (slrEnabled && await slrHandler.HandleIfPreviouslyFailed(context).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                if (slrEnabled) //TODO: why do we have this thing here :/?
                {
                    context.Message.Headers.Remove(Headers.Retries);
                }

                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                //NOTE: this is a behavior change. If we fail here we go directly to error. Previously slr would fire
                if (flrEnabled && await flrHandler.HandleMessageFailure(context, ex).ConfigureAwait(false))
                {
                    return;
                }

                if (slrEnabled)
                {
                    slrHandler.MarkForFutureDeferal(context, ex);

                    return;
                }
                    
                throw;
            }
        }

        FirstLevelRetriesHandler flrHandler;
        SecondLevelRetriesHandler slrHandler;
        MoveFaultsToErrorQueueHandler errorHandler;

        bool flrEnabled;
        bool slrEnabled;
        readonly bool runningWithTransactions;
    }
}