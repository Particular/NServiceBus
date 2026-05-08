namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Pipeline;

class TestRunIdSkipBehavior(ScenarioContext scenarioContext) : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        var testRunId = scenarioContext.TestRunId.ToString();

        if (context.Message.Headers.TryGetValue("$AcceptanceTesting.TestRunId", out var runId) && runId != testRunId)
        {
            TestContext.Out.WriteLine($"Skipping message {context.Message.MessageId} from previous test run");
            return Task.CompletedTask;
        }

        return next(context);
    }
}
