#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_terminal_recoverability_failures_from_multiple_branches
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var firstFailure = new InvalidOperationException("first");
        var secondFailure = new ArgumentException("second");

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                if (!messageContext.Headers.TryGetValue("kind", out var kind) || kind == "parent")
                {
                    _ = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "first-child"
                    })), messageContext.TransportTransaction, cancellationToken);

                    _ = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "second-child"
                    })), messageContext.TransportTransaction, cancellationToken);

                    return;
                }

                throw kind == "first-child" ? firstFailure : secondFailure;
            },
            (errorContext, _) =>
            {
                InlineExecutionTestHelper.SetRecoverabilityAction(errorContext, new Discard("inline failure"));
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
        {
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            ["kind"] = "parent"
        })), new TransportTransaction());

        var exception = await InlineExecutionTestHelper.CatchException(rootTask);

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.InstanceOf<AggregateException>());
            Assert.That(exception, Is.AnyOf(firstFailure, secondFailure));
        });

        await receiver.StopReceive();
    }
}
