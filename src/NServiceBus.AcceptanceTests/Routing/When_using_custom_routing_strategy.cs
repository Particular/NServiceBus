﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    public class When_using_custom_routing_strategy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_route_commands_correctly()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b =>
                    b.When(c => c.EndpointsStarted, async session =>
                    {
                        await session.Send(new MyCommand { Instance = Discriminator1 });
                        await session.Send(new MyCommand { Instance = Discriminator2 });
                        await session.Send(new MyCommand { Instance = Discriminator1 });
                        await session.Send(new MyCommand { Instance = Discriminator2 });
                    })
                )
                .WithEndpoint<Receiver>(b => b.CustomConfig(cfg => cfg.MakeInstanceUniquelyAddressable(Discriminator1)))
                .WithEndpoint<Receiver>(b => b.CustomConfig(cfg => cfg.MakeInstanceUniquelyAddressable(Discriminator2)))
                .Done(c => c.MessageDeliveredReceiver1 >= 2 && c.MessageDeliveredReceiver2 >=2)
                .Run();

            Assert.AreEqual(2, ctx.MessageDeliveredReceiver1);
            Assert.AreEqual(2, ctx.MessageDeliveredReceiver2);
        }

        static string Discriminator1 = "553E9649";
        static string Discriminator2 = "F9D0022C";

        public class Context : ScenarioContext
        {
            public int MessageDeliveredReceiver1;
            public int MessageDeliveredReceiver2;
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                var receiverEndpoint = Conventions.EndpointNamingConvention(typeof(Receiver));

                EndpointSetup<DefaultServer>(c =>
                {
                    c.GetSettings().GetOrCreate<UnicastRoutingTable>()
                        .AddOrReplaceRoutes("CustomRoutingFeature", new List<RouteTableEntry>
                        {
                            new RouteTableEntry(typeof(MyCommand), UnicastRoute.CreateFromEndpointName(receiverEndpoint))
                        });
                    c.GetSettings().GetOrCreate<EndpointInstances>()
                        .AddOrReplaceInstances("CustomRoutingFeature", new List<EndpointInstance>
                        {
                            new EndpointInstance(receiverEndpoint, Discriminator1),
                            new EndpointInstance(receiverEndpoint, Discriminator2)
                        });
                    c.GetSettings().GetOrCreate<DistributionPolicy>()
                        .SetDistributionStrategy(new ContentBasedRoutingStrategy(receiverEndpoint));
                });
            }

            class ContentBasedRoutingStrategy : DistributionStrategy
            {
                public ContentBasedRoutingStrategy(string endpoint) : base(endpoint, DistributionStrategyScope.Send)
                {
                }

                public override string SelectReceiver(string[] receiverAddresses)
                {
                    throw new NotImplementedException(); // should never be called
                }

                public override string SelectDestination(DistributionContext context)
                {
                    var message = context.Message.Instance as MyCommand;
                    if (message != null)
                    {
                        var address = context.ToTransportAddress(new EndpointInstance(Endpoint, message.Instance));
                        return context.ReceiverAddresses.Single(a => a.Contains(address));
                    }
                    throw new InvalidOperationException("Unable to route!");
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyCommandHandler : IHandleMessages<MyCommand>
            {
                public MyCommandHandler(Context testContext, ReadOnlySettings settings)
                {
                    this.testContext = testContext;
                    this.settings = settings;
                }

                public Task Handle(MyCommand message, IMessageHandlerContext context)
                {
                    var instanceDiscriminator = settings.Get<string>("EndpointInstanceDiscriminator");

                    if (instanceDiscriminator == Discriminator1)
                    {
                        Interlocked.Increment(ref testContext.MessageDeliveredReceiver1);
                    }
                    if (instanceDiscriminator == Discriminator2)
                    {
                        Interlocked.Increment(ref testContext.MessageDeliveredReceiver2);
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
                ReadOnlySettings settings;
            }
        }

        public class MyCommand : ICommand
        {
            public string Instance { get; set; } 
        }
    }
}