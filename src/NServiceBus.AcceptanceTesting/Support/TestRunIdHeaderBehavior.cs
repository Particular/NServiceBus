namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Pipeline;

class TestRunIdHeaderBehavior(ScenarioContext scenarioContext) : IBehavior<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>
{
    public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
    {
        context.Headers["$AcceptanceTesting.TestRunId"] = scenarioContext.TestRunId.ToString();
        return next(context);
    }
}
