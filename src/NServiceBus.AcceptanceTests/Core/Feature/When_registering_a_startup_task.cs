namespace NServiceBus.AcceptanceTests.Core.Feature
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_registering_a_startup_task : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task The_endpoint_should_start()
        {
            var context = await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<SendOnlyEndpoint>()
                .Done(c => c.SendOnlyEndpointWasStarted)
                .Run();

            Assert.True(context.SendOnlyEndpointWasStarted, "The endpoint should have started without any errors");
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public Guid Id { get; set; }

            public bool SendOnlyEndpointWasStarted { get; set; }
        }

        public class SendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public SendOnlyEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendOnly();
                    c.EnableFeature<Bootstrapper>();
                });
            }

            public class Bootstrapper : Feature
            {
                public Bootstrapper()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(b => new MyTask(b.GetService<Context>()));
                }

                public class MyTask : FeatureStartupTask
                {
                    public MyTask(Context scenarioContext)
                    {
                        this.scenarioContext = scenarioContext;
                    }

                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        scenarioContext.SendOnlyEndpointWasStarted = true;
                        return Task.FromResult(0);
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        return Task.FromResult(0);
                    }

                    readonly Context scenarioContext;
                }
            }
        }
    }
}