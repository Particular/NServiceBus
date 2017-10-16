namespace NServiceBus.AcceptanceTests.Core.Installers
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using FakeTransport;
    using NUnit.Framework;

    public class When_creating_queues : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_create_queues_when_queue_creation_disabled_and_installers_disabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.DoNotCreateQueues = true;
                    c.EnableInstallers = false;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.QueuesCreated);
        }

        [Test]
        public async Task Should_not_create_queues_when_queue_creation_disabled_and_installers_enabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.DoNotCreateQueues = true;
                    c.EnableInstallers = true;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.QueuesCreated);
        }

        [Test]
        public async Task Should_not_create_queues_when_queue_creation_enabled_and_installers_disabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.DoNotCreateQueues = false;
                    c.EnableInstallers = false;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.QueuesCreated);
        }

        [Test]
        public async Task Should_create_queues_when_queue_creation_enabled_and_installers_enabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.DoNotCreateQueues = false;
                    c.EnableInstallers = true;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.QueuesCreated);
        }

        class Context : ScenarioContext
        {
            public bool DoNotCreateQueues { get; set; }
            public bool EnableInstallers { get; set; }
            public bool QueuesCreated { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((c, t) =>
                {
                    c.UseTransport<FakeTransport>()
                        .WhenQueuesCreated(() =>
                        {
                            t.QueuesCreated = true;
                        });

                    if (t.DoNotCreateQueues)
                    {
                        c.DoNotCreateQueues();
                    }
                    
                    if (!t.EnableInstallers)
                    {
                        // DefaultServer always calls EnableInstaller. We need to reverse it.
                        c.GetSettings().Set("Installers.Enable", false);
                    }
                });
            }
        }
    }
}