#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_discard_recoverability_faults_scope_with_original_exception
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var processingException = new InvalidOperationException("boom");

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => throw processingException,
            (errorContext, _) =>
            {
                SetRecoverabilityAction(errorContext, new Discard("inline failure"));
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());
        var exception = await CatchException(rootTask);

        Assert.That(exception, Is.SameAs(processingException));

        await receiver.StopReceive();
    }
}