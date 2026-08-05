#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

class RecoverabilityPipelineExecutor<TState>(
    IServiceProvider serviceProvider,
    IPipelineCache pipelineCache,
    MessageOperations messageOperations,
    RecoverabilityConfig recoverabilityConfig,
    Func<ErrorContext, TState, RecoverabilityAction> recoverabilityPolicy,
    IPipeline<IRecoverabilityContext> recoverabilityPipeline,
    FaultMetadataExtractor faultMetadataExtractor,
    TState state,
    IActivityFactory activityFactory) : IRecoverabilityPipelineExecutor
{
    public async Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default)
    {
        var childScope = serviceProvider.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            RecoverabilityAction? recoverabilityAction;

            using (var activity = activityFactory.StartRecoverabilityActivity(errorContext))
            {
                recoverabilityAction = recoverabilityPolicy(errorContext, state);

                if (activity is not null)
                {
                    activityFactory.UpdateActivityFromRecoverabilityAction(activity, recoverabilityAction, errorContext.ReceiveAddress);
                }

                var logMessage = recoverabilityAction switch
                {
                    ImmediateRetry => $"Immediate Retry is going to retry message '{errorContext.MessageId}' because of an exception:",
                    DelayedRetry delayedRetry => $"Delayed Retry will reschedule message '{errorContext.MessageId}' after a delay of {delayedRetry.Delay} because of an exception:",
                    MoveToError moveToError => $"Moving message '{errorContext.MessageId}' to the error queue '{moveToError.ErrorQueue}' because processing failed due to an exception:",
                    Discard discard => $"Discarding message with id '{errorContext.MessageId}'. Reason: {discard.Reason}",
                    _ => $"Recoverability action '{recoverabilityAction.GetType().Name}' invoked for message '{errorContext.MessageId}'."
                };

                if (activityFactory.Options.ExceptionRecordingMode == ExceptionRecordingMode.SpanAndLogs)
                {
                    Logger.Info(logMessage, errorContext.Exception);
                }
                else
                {
                    Logger.Info(logMessage);
                }
            }

            var metadata = faultMetadataExtractor.Extract(errorContext);

            var recoverabilityContext = new RecoverabilityContext(
                    childScope.ServiceProvider,
                    messageOperations,
                    pipelineCache,
                    errorContext,
                    recoverabilityConfig,
                    metadata,
                    recoverabilityAction,
                    errorContext.Extensions,
                    cancellationToken);

            await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);

            return recoverabilityContext.RecoverabilityAction.ErrorHandleResult;
        }
    }

    static readonly ILog Logger = LogManager.GetLogger<IRecoverabilityPipelineExecutor>();
}