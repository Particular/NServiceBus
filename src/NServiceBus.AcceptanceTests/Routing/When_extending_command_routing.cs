﻿namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_extending_command_routing : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpoint => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_route_commands_correctly()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b =>
                    b.When(c => c.EndpointsStarted, async session =>
                    {
                        await session.Send(new MyCommand());
                        await session.Send(new MyCommand());
                        await session.Send(new MyCommand());
                        await session.Send(new MyCommand());
                    })
                )
                .WithEndpoint<Receiver>()
                .Done(c => c.MessageDelivered >= 4)
                .Run();

            Assert.IsTrue(ctx.MessageDelivered >= 4);
        }

        public class Context : ScenarioContext
        {
            public int MessageDelivered;
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CustomRoutingFeature : Feature
            {
                public CustomRoutingFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RoutingTable().AddOrReplaceRoutes("CustomRoutingFeature", new List<RouteTableEntry>
                    {
                        new RouteTableEntry(typeof(MyCommand), UnicastRoute.CreateFromEndpointName(ReceiverEndpoint))
                    });
                    context.EndpointInstances().AddOrReplaceInstances("CustomRoutingFeature", new List<EndpointInstance>()
                    {
                        new EndpointInstance(ReceiverEndpoint, "XYZ"),
                        new EndpointInstance(ReceiverEndpoint, "ABC")
                    });
                    context.DistributionPolicy().SetDistributionStrategy(ReceiverEndpoint, new XyzDistributionStrategy());
                }

                class XyzDistributionStrategy : DistributionStrategy
                {
                    public override UnicastRoutingTarget SelectDestination(UnicastRoutingTarget[] allInstances)
                    {
                        return allInstances.First(x => x.Instance.Discriminator.Contains("XYZ"));
                    }
                }
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.MakeInstanceUniquelyAddressable("XYZ"));
            }

            public class MyCommandHandler : IHandleMessages<MyCommand>
            {
                public Context Context { get; set; }

                public Task Handle(MyCommand evnt, IMessageHandlerContext context)
                {
                    Interlocked.Increment(ref Context.MessageDelivered);
                    return Task.FromResult(0);
                }
            }
        }

        public class MyCommand : ICommand
        {
        }
    }
}