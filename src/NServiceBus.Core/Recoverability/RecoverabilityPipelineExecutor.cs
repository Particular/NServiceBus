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
        using var activity = activityFactory.StartRecoverabilityActivity(errorContext);

        var childScope = serviceProvider.CreateAsyncScope();
        await using (childScope.ConfigureAwait(false))
        {
            var recoverabilityAction = recoverabilityPolicy(errorContext, state);

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

                RecoverabilityActionLogger.LogRecoverabilityAction(serviceProvider, recoverabilityContext.RecoverabilityAction, errorContext);
            }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - recording and rethrowing
            catch (Exception ex)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
            {
                activityFactory.RecordError(activity, ex, recoverabilityContext.Extensions);
                throw;
            }
            finally
            {
                EnrichActivity(activity, recoverabilityContext.RecoverabilityAction, errorContext.ReceiveAddress);
            }

            return recoverabilityContext.RecoverabilityAction.ErrorHandleResult;
        }
    }

    void EnrichActivity(Activity? activity, RecoverabilityAction recoverabilityAction, string receiveAddress)
    {
        if (activity is null)
        {
            return;
        }

        try
        {
            activityFactory.EnrichActivityWithRecoverabilityActionData(activity, recoverabilityAction, receiveAddress);
        }
#pragma warning disable PS0019 // Do not catch Exception without considering OperationCanceledException - tracing must not change the recoverability outcome
        catch (Exception)
#pragma warning restore PS0019 // Do not catch Exception without considering OperationCanceledException
        {
            // A failure to enrich the activity must neither fail recoverability nor replace the exception of the recoverability pipeline.
        }
    }
}