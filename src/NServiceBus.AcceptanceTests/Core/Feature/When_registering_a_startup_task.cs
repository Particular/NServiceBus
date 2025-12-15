namespace NServiceBus.AcceptanceTests.Core.Feature;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_registering_a_startup_task : NServiceBusAcceptanceTest
{
    [Test]
    public async Task The_endpoint_should_start()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SendOnlyEndpoint>()
            .Run();

        Assert.That(context.SendOnlyEndpointWasStarted, Is.True, "The endpoint should have started without any errors");
    }

    public class Context : ScenarioContext
    {
        public bool SendOnlyEndpointWasStarted { get; set; }
    }

    public class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendOnly();
                c.EnableFeature<Bootstrapper>();
            });

        public class Bootstrapper : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask<MyTask>();

            class MyTask(Context scenarioContext) : FeatureStartupTask
            {
                protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    scenarioContext.SendOnlyEndpointWasStarted = true;
                    scenarioContext.MarkAsCompleted();
                    return Task.CompletedTask;
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }
}