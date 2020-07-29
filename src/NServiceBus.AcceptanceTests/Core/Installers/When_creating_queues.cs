namespace NServiceBus.AcceptanceTests.Core.Installers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using FakeTransport;
    using Features;
    using NUnit.Framework;
    using Transport;

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

        [Test]
        public async Task Should_setup_queue_bindings_prior_to_creating_queues()
        {
            var instanceDiscriminator = "myInstance";
            var context = await Scenario.Define<Context>(c =>
                {
                    c.EnableInstallers = true;
                })
                .WithEndpoint<Endpoint>(e => e.CustomConfig(endpointConfig =>
                 {
                     endpointConfig.AuditProcessedMessagesTo("myAudit");
#pragma warning disable 618
                     endpointConfig.ForwardReceivedMessagesTo("myForwardingEndpoint");
#pragma warning restore 618
                     endpointConfig.MakeInstanceUniquelyAddressable(instanceDiscriminator);
                 }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.QueuesCreated);

            CollectionAssert.AreEqual(new List<string>
            {
               "myAudit",
               "myForwardingEndpoint",
               "error"
            }, context.SendingAddresses);

            var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint));

            CollectionAssert.AreEqual(new List<string>
            {
                endpointName, //main input queue
                $"{endpointName}-{instanceDiscriminator}", // instance-specific queue
                "MySatelliteAddress"
            }, context.ReceivingAddresses);
        }

        class Context : ScenarioContext
        {
            public bool DoNotCreateQueues { get; set; }
            public bool EnableInstallers { get; set; }
            public bool QueuesCreated { get; set; }
            public List<string> ReceivingAddresses { get; set; }
            public List<string> SendingAddresses { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((c, t) =>
                {
                    c.UseTransport<FakeTransport>()
                        .WhenQueuesCreated(bindings =>
                        {
                            t.SendingAddresses = bindings.SendingAddresses.ToList();
                            t.ReceivingAddresses = bindings.ReceivingAddresses.ToList();
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

            class MySatellite : Feature
            {
                public MySatellite()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.AddSatelliteReceiver("MySatellite",
                        "MySatelliteAddress",
                        PushRuntimeSettings.Default,
                        (_, __) => throw new NotImplementedException(),
                        (_, __) => throw new NotImplementedException());
                }
            }
        }
    }
}