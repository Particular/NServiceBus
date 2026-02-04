namespace NServiceBus.AcceptanceTests.Routing;

using System;
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
                b.When(async session =>
                {
                    await session.Send(new MyCommand { Instance = Discriminator1 });
                    await session.Send(new MyCommand { Instance = Discriminator2 });
                    await session.Send(new MyCommand { Instance = Discriminator1 });
                    await session.Send(new MyCommand { Instance = Discriminator1 });
                })
            )
            .WithEndpoint<Receiver>(b => b.CustomConfig(cfg => cfg.MakeInstanceUniquelyAddressable(Discriminator1)))
            .WithEndpoint<Receiver>(b => b.CustomConfig(cfg => cfg.MakeInstanceUniquelyAddressable(Discriminator2)))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.MessageDeliveredReceiver1, Is.EqualTo(3));
            Assert.That(ctx.MessageDeliveredReceiver2, Is.EqualTo(1));
        }
    }

    const string Discriminator1 = "553E9649";
    const string Discriminator2 = "F9D0022C";

    public class Context : ScenarioContext
    {
        public int MessageDeliveredReceiver1;
        public int MessageDeliveredReceiver2;

        public void MaybeCompleted() => MarkAsCompleted(MessageDeliveredReceiver1 >= 3, MessageDeliveredReceiver2 >= 1);
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender()
        {
            var receiverEndpoint = Conventions.EndpointNamingConvention(typeof(Receiver));

            EndpointSetup<DefaultServer>(c =>
            {
                c.GetSettings().GetOrCreate<UnicastRoutingTable>()
                    .AddOrReplaceRoutes("CustomRoutingFeature",
                    [
                        new RouteTableEntry(typeof(MyCommand), UnicastRoute.CreateFromEndpointName(receiverEndpoint))
                    ]);
                c.GetSettings().GetOrCreate<EndpointInstances>()
                    .AddOrReplaceInstances("CustomRoutingFeature",
                    [
                        new EndpointInstance(receiverEndpoint, Discriminator1),
                        new EndpointInstance(receiverEndpoint, Discriminator2)
                    ]);
                c.GetSettings().GetOrCreate<DistributionPolicy>()
                    .SetDistributionStrategy(new ContentBasedRoutingStrategy(receiverEndpoint));
            });
        }

        class ContentBasedRoutingStrategy(string endpoint) : DistributionStrategy(endpoint, DistributionStrategyScope.Send)
        {
            public override string SelectDestination(DistributionContext context)
            {
                if (context.Message.Instance is MyCommand message)
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
        public Receiver() => EndpointSetup<DefaultServer>();

        [Handler]
        public class MyCommandHandler(Context testContext, IReadOnlySettings settings) : IHandleMessages<MyCommand>
        {
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

                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyCommand : ICommand
    {
        public string Instance { get; set; }
    }
}