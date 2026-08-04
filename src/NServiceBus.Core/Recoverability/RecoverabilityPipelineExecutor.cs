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

                var logMessage = recoverabilityAction.LogMessage(errorContext.MessageId);

                if (activityFactory.Options.ExceptionRecordingMode == ExceptionRecordingMode.Dup)
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