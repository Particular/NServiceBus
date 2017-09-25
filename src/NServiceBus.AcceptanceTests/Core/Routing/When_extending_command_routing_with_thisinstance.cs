namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    public class When_extending_command_routing_with_thisinstance : NServiceBusAcceptanceTest
    {
        const string Discriminator2 = "2";
        const string Discriminator1 = "1";
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Endpoint));

        [Test]
        public async Task Should_route_according_to_distribution_strategy()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(c => c.EndpointsStarted, session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.RouteToThisEndpoint();
                    return session.Send(new MyCommand(), sendOptions);
                }).CustomConfig(c => c.MakeInstanceUniquelyAddressable(Discriminator1)))
                .WithEndpoint<Endpoint>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable(Discriminator2)))
                .Done(c => c.MessageDelivered >= 1)
                .Run();

            Assert.AreEqual(1, ctx.MessageDelivered);
            Assert.IsTrue(ctx.StrategyCalled);
        }

        public class Context : ScenarioContext
        {
            public int MessageDelivered;
            public bool StrategyCalled;
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
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
                            new EndpointInstance(ReceiverEndpoint, Discriminator2)
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
                    if (settings.Get<string>("EndpointInstanceDiscriminator") == Discriminator2)
                    {
                        Interlocked.Increment(ref testContext.MessageDelivered);
                    }
                    return Task.FromResult(0);
                }
            }

            class SelectFirstDistributionStrategy : DistributionStrategy
            {
                Context testContext;

                public SelectFirstDistributionStrategy(string endpoint, Context testContext) : base(endpoint, DistributionStrategyScope.Send)
                {
                    this.testContext = testContext;
                }

                public override string SelectDestination(DistributionContext context)
                {
                    testContext.StrategyCalled = true;
                    return context.ReceiverAddresses[0];
                }
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}