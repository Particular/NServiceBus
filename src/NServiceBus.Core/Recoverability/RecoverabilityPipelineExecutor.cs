#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
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

                if (recoverabilityAction is ImmediateRetry)
                {
                    activity?.AddTag(ActivityTags.RecoverabilityAction, "immediate_retry");
                    activity?.DisplayName += " immediate retry";
                }
                else if (recoverabilityAction is DelayedRetry)
                {
                    activity?.AddTag(ActivityTags.RecoverabilityAction, "delayed_retry");
                    activity?.DisplayName += " delayed retry";
                }
                else if (recoverabilityAction is MoveToError)
                {
                    activity?.AddTag(ActivityTags.RecoverabilityAction, "move_to_error");
                    activity?.DisplayName += " move to error queue";
                }
                else if (recoverabilityAction is Discard)
                {
                    activity?.AddTag(ActivityTags.RecoverabilityAction, "discard");
                    activity?.DisplayName += " discard";
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
}