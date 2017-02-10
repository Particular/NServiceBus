namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    public class When_extending_command_routing_with_thisinstance : NServiceBusAcceptanceTest
    {
        const string Descriminator2 = "2";
        const string Descriminator1 = "1";
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_route_according_to_distribution_strategy()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>(b => b.When(c => c.EndpointsStarted, session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    return session.Send(new MyCommand(), sendOptions);
                }).CustomConfig(c => c.MakeInstanceUniquelyAddressable(Descriminator1)))
                .WithEndpoint<Receiver>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable(Descriminator2)))
                .Done(c => c.MessageDelivered >= 1)
                .Run(TimeSpan.FromSeconds(10));

            Assert.AreEqual(1, ctx.MessageDelivered);
            Assert.IsTrue(ctx.StrategyCalled);
        }

        public class Context : ScenarioContext
        {
            public int MessageDelivered;
            public bool StrategyCalled;
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.GetSettings().GetOrCreate<UnicastRoutingTable>()
                        .AddOrReplaceRoutes("CustomRoutingFeature", new List<RouteTableEntry>
                        {
                            new RouteTableEntry(typeof(MyCommand), UnicastRoute.CreateFromEndpointName(ReceiverEndpoint))
                        });
                    c.GetSettings().GetOrCreate<EndpointInstances>()
                        .AddOrReplaceInstances("CustomRoutingFeature", new List<EndpointInstance>
                        {
                            new EndpointInstance(ReceiverEndpoint, Descriminator2)
                        });
                    c.GetSettings().GetOrCreate<DistributionPolicy>()
                        .SetDistributionStrategy(new SelectFirstDistributionStrategy(ReceiverEndpoint, (Context)ScenarioContext));
                });
            }

            public class MyCommandHandler : IHandleMessages<MyCommand>
            {
                Context testContext;
                ReadOnlySettings settings;

                public MyCommandHandler(Context context, ReadOnlySettings settings)
                {
                    this.settings = settings;
                    testContext = context;
                }

                public Task Handle(MyCommand message, IMessageHandlerContext context)
                {
                    if (settings.Get<string>("EndpointInstanceDiscriminator") == Descriminator2)
                    {
                        Interlocked.Increment(ref testContext.MessageDelivered);
                    }
                    return Task.FromResult(0);
                }
            }

            class SelectFirstDistributionStrategy : DistributionStrategy
            {
                Context context;

                public SelectFirstDistributionStrategy(string endpoint, Context context) : base(endpoint, DistributionStrategyScope.Send)
                {
                    this.context = context;
                }

                public override string SelectReceiver(string[] receiverAddresses)
                {
                    context.StrategyCalled = true;
                    return receiverAddresses[0];
                }
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}