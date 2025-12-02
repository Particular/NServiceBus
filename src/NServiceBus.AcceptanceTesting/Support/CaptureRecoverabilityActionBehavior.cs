#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Faults;
using Pipeline;

class CaptureRecoverabilityActionBehavior(string endpointName, ScenarioContext scenarioContext)
    : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    public async Task Invoke(IRecoverabilityContext context, Func<IRecoverabilityContext, Task> next)
    {
        await next(context).ConfigureAwait(false);

        switch (context.RecoverabilityAction)
        {
            case MoveToError moveToErrorAction:
                {
                    var failedMessage = new FailedMessage(
                        context.FailedMessage.MessageId,
                        new Dictionary<string, string>(context.FailedMessage.Headers),
                        context.FailedMessage.Body,
                        context.Exception,
                        moveToErrorAction.ErrorQueue);

                    _ = scenarioContext.FailedMessages.AddOrUpdate(
                        endpointName, static (_, failedMessage) => [failedMessage],
                        static (_, failed, failedMessage) => [.. failed, failedMessage], failedMessage);

                    MarkMessageAsCompleted();
                    break;
                }
            case Discard:
                MarkMessageAsCompleted();
                break;
            default:
                break;
        }

        return;

        void MarkMessageAsCompleted() => scenarioContext.UnfinishedFailedMessages.AddOrUpdate(context.FailedMessage.MessageId, _ => false, static (_, _) => false);
    }
}