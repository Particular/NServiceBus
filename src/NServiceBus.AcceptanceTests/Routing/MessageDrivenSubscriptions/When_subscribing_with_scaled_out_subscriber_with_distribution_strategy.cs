namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    public class When_subscribing_with_scaled_out_subscriber_with_distribution_strategy : NServiceBusAcceptanceTest
    {
        static string Discriminator1 = "553E9619";
        static string Discriminator2 = "F9D0023C";

        [Test]
        public async Task Should_honor_strategy()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.PublisherReceivedSubscription.Count >= 2, async s =>
                {
                    await s.Publish(new MyEvent());
                    await s.Publish(new MyEvent());
                }))
                .WithEndpoint<Subscriber>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable(Discriminator1)).When(s => s.Subscribe<MyEvent>()))
                .WithEndpoint<Subscriber>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable(Discriminator2)).When(s => s.Subscribe<MyEvent>()))
                .Done(c => c.HandledOnDistriminator.Count >= 2)
                .Run();

            Assert.That(context.HandledOnDistriminator, Does.Contain(Discriminator1));
            Assert.That(context.HandledOnDistriminator, Does.Contain(Discriminator1));
            Assert.That(context.HandledOnDistriminator.Count, Is.EqualTo(2));

            Assert.That(context.ReceiverAddresses.Count, Is.EqualTo(2));
            Assert.That(context.PublisherReceivedSubscription.Count, Is.EqualTo(2));

            Assert.That(context.PublisherReceivedSubscription, Does.Contain(context.ReceiverAddresses.ElementAt(0)));
            Assert.That(context.PublisherReceivedSubscription, Does.Contain(context.ReceiverAddresses.ElementAt(1)));
        }

        class Context : ScenarioContext
        {
            public List<string> HandledOnDistriminator { get; } = new List<string>();
            public List<string> PublisherReceivedSubscription { get; } = new List<string>();
            public HashSet<string> ReceiverAddresses { get; } = new HashSet<string>();
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.GetSettings().GetOrCreate<DistributionPolicy>().SetDistributionStrategy(new SelectFirstDistributionStrategy((Context) ScenarioContext, Conventions.EndpointNamingConvention(typeof(Subscriber)), DistributionStrategyScope.Publish));
                    c.OnEndpointSubscribed((SubscriptionEventArgs args, Context ctx) =>
                    {
                        ctx.PublisherReceivedSubscription.Add(args.SubscriberReturnAddress);
                    });
                });
            }

            class SelectFirstDistributionStrategy : DistributionStrategy
            {
                Context testContext;

                public SelectFirstDistributionStrategy(Context context, string endpoint, DistributionStrategyScope scope) : base(endpoint, scope)
                {
                    testContext = context;
                }

                public override string SelectReceiver(string[] receiverAddresses)
                {
                    throw new NotImplementedException();
                }

                public override string SelectDestination(DistributionContext context)
                {
                    foreach (var receiverAddress in context.ReceiverAddresses)
                    {
                        testContext.ReceiverAddresses.Add(receiverAddress);
                    }

                    var address = context.ToTransportAddress(new EndpointInstance(Endpoint, Discriminator1));
                    return context.ReceiverAddresses.Single(a => a == address);
                }
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    // currently there is no way to call RoutingSettings<T> extensions
                    config.GetSettings().Set("SubscribeWithInstanceSpecificQueue", true);

                    config.DisableFeature<AutoSubscribe>();
                }, metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            class MyHandler : IHandleMessages<MyEvent>
            {
                Context testContext;
                ReadOnlySettings settings;

                public MyHandler(Context context, ReadOnlySettings settings)
                {
                    this.settings = settings;
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    var discriminator = settings.Get<string>("EndpointInstanceDiscriminator");
                    testContext.HandledOnDistriminator.Add(discriminator);
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}