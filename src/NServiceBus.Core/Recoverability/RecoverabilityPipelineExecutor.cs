#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics;
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
        var childScope = serviceProvider.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            RecoverabilityAction? recoverabilityAction;

            // Disposing the recoverability activity does not bring the ambient activity back when it started a new
            // trace, see ActivityExtensions.RestoreAmbientActivity. The recoverability pipeline below runs under the
            // ambient activity either way.
            var ambientActivity = Activity.Current;
            try
            {
                using var activity = activityFactory.StartRecoverabilityActivity(errorContext);

                recoverabilityAction = recoverabilityPolicy(errorContext, state);

                if (activity is not null)
                {
                    activityFactory.UpdateActivityFromRecoverabilityAction(activity, recoverabilityAction, errorContext.ReceiveAddress);
                }

                recoverabilityActionLogger.LogRecoverabilityAction(recoverabilityAction, errorContext, activityFactory.Options.ExceptionRecordingMode);
            }
            finally
            {
                ActivityExtensions.RestoreAmbientActivity(ambientActivity);
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

    readonly RecoverabilityActionLogger recoverabilityActionLogger = new(serviceProvider);
}
