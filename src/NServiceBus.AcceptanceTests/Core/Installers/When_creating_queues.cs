namespace NServiceBus.AcceptanceTests.Core.Installers
{
    using System;
    using System.Collections.Generic;
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
        public async Task Should_not_create_queues_when_installers_disabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.EnableInstallers = false;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsFalse(context.SetupInfrastructure);
        }

        [Test]
        public async Task Should_create_queues_when_installers_enabled()
        {
            var context = await Scenario.Define<Context>(c =>
                {
                    c.EnableInstallers = true;
                })
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.SetupInfrastructure);
        }

        [Test]
        public async Task Should_pass_receive_and_send_queue_addresses()
        {
            var instanceDiscriminator = "myInstance";
            var context = await Scenario.Define<Context>(c =>
                {
                    c.EnableInstallers = true;
                })
                .WithEndpoint<Endpoint>(e => e.CustomConfig(endpointConfig =>
                 {
                     endpointConfig.AuditProcessedMessagesTo("myAudit");
                     endpointConfig.MakeInstanceUniquelyAddressable(instanceDiscriminator);
                 }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.SetupInfrastructure);

            CollectionAssert.AreEqual(new List<string>
            {
               "myAudit",
               "error"
            }, context.SendingAddresses);

            var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint));

            CollectionAssert.AreEquivalent(new List<string>
            {
                endpointName, //main input queue
                $"{endpointName}-{instanceDiscriminator}", // instance-specific queue
                "MySatelliteAddress"
            }, context.ReceivingAddresses);
        }

        class Context : ScenarioContext
        {
            public bool EnableInstallers { get; set; }
            public string[] ReceivingAddresses { get; set; }
            public string[] SendingAddresses { get; set; }
            public bool SetupInfrastructure { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer, Context>((c, t) =>
                {
                    var fakeTransport = new FakeTransport
                    {
                        OnTransportInitialize = queues =>
                        {
                            t.SendingAddresses = queues.sendingAddresses;
                            t.ReceivingAddresses = queues.receivingAddresses;
                            t.SetupInfrastructure = queues.setupInfrastructure;
                        }
                    };
                    c.UseTransport(fakeTransport);

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
                        (_, __, ___) => throw new NotImplementedException());
                }
            }
        }
    }
}