namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Faults;
using Pipeline;

class CaptureRecoverabilityActionBehavior : IBehavior<IRecoverabilityContext, IRecoverabilityContext>
{
    readonly string endpointName;
    readonly ScenarioContext scenarioContext;

    public CaptureRecoverabilityActionBehavior(string endpointName, ScenarioContext scenarioContext)
    {
        this.endpointName = endpointName;
        this.scenarioContext = scenarioContext;
    }

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

                    scenarioContext.FailedMessages.AddOrUpdate(
                        endpointName,
                        new[]
                        {
                            failedMessage
                        },
                        (i, failed) =>
                        {
                            var result = failed.ToList();
                            result.Add(failedMessage);
                            return result;
                        });

                    MarkMessageAsCompleted();
                    break;
                }
            case Discard discardAction:
                MarkMessageAsCompleted();
                break;
            default:
                break;
        }

        void MarkMessageAsCompleted()
        {
            scenarioContext.UnfinishedFailedMessages.AddOrUpdate(context.FailedMessage.MessageId,
                id => false,
                (id, value) => false);
        }
    }
}