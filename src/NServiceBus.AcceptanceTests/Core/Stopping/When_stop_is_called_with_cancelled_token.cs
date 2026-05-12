#nullable enable

namespace NServiceBus.AcceptanceTests.Core.Stopping;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_stop_is_called_with_cancelled_token : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_complete_shutdown_gracefully()
    {
        // Before the fix, when Stop was called with an already-cancelled token,
        // stopSemaphore.WaitAsync(cancelledToken) threw OperationCanceledException before
        // entering the critical section, leaving status as Running. The framework then
        // called DisposeAsync -> Stop(None), which re-entered and attempted full shutdown
        // against a DI container already torn down, causing ObjectDisposedException on
        // stoppingTokenSource or using a disposed ILoggerFactory.
        //
        // This test cancels the Run token after the endpoint starts, which causes
        // StopEndpoints to call Stop with an already-canceled token, then DisposeAsync
        // in the finally block. This is the same scenario that triggered the bug in
        // NServiceBus.Metrics.ServiceControl acceptance tests.
        using var cts = new CancellationTokenSource();
        Context? context = null;

        await Assert.ThatAsync(async () =>
        {
            await Scenario.Define<Context>(ctx =>
                {
                    context = ctx;
                })
                .WithEndpoint<Endpoint>(b => b
                    .Resolves(async (_, _, _) =>
                    {
                        // Cancel the Run token so that StopEndpoints receives an
                        // already-canceled token, reproducing the race condition.
                        await cts.CancelAsync();
                    }, afterStart: true))
                .Run(cts.Token);
        }, Throws.TypeOf<OperationCanceledException>().Or.TypeOf<TimeoutException>());
        Assert.That(context?.EndpointsStarted.Task.IsCompletedSuccessfully, Is.True, "Endpoints should have started successfully");
    }

    class Context : ScenarioContext;

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();
    }
}