#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
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
        // The activity covers the whole recoverability execution: the decision and the pipeline that carries it
        // out (the retry dispatch or the move to the error queue), so its duration is the time the failed message
        // spent in recoverability.
        using var activity = activityFactory.StartRecoverabilityActivity(errorContext);

        var childScope = serviceProvider.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            var recoverabilityAction = recoverabilityPolicy(errorContext, state);

            if (activity is not null)
            {
                activityFactory.UpdateActivityFromRecoverabilityAction(activity, recoverabilityAction, errorContext.ReceiveAddress);
            }

            recoverabilityActionLogger.LogRecoverabilityAction(recoverabilityAction, errorContext, activityFactory.Options.ExceptionRecordingMode);

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

            try
            {
                await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - recording and rethrowing
            catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
            {
                activityFactory.RecordError(activity, ex, childScope.ServiceProvider);
                throw;
            }

            return recoverabilityContext.RecoverabilityAction.ErrorHandleResult;
        }
    }

    readonly RecoverabilityActionLogger recoverabilityActionLogger = new(serviceProvider);
}
