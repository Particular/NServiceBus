namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_subscribing_to_a_derived_event : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Base_event_should_not_be_delivered()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
                {
                    await session.Publish<IBaseEvent>();
                    await session.Send(new Done());
                }))
                .WithEndpoint<Subscriber>(b => b.When(async (session, c) => await session.Subscribe<SpecificEvent>()))
                .Done(c => c.Done)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(c => Assert.IsFalse(c.SubscriberGotEvent))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotEvent { get; set; }

            public bool SubscriberSubscribed { get; set; }

            public bool Done { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) =>
                {
                    context.SubscriberSubscribed = true;
                }))
                    .AddMapping<Done>(typeof(Subscriber));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.LimitMessageProcessingConcurrencyTo(1); //To ensure Done is processed after the event.
                },
                metadata => metadata.RegisterPublisherFor<SpecificEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<SpecificEvent>
            {
                public Context Context { get; set; }

                public Task Handle(SpecificEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.SubscriberGotEvent = true;
                    return Task.FromResult(0);
                }
            }

            public class DoneHandler : IHandleMessages<Done>
            {
                public Context Context { get; set; }

                public Task Handle(Done message, IMessageHandlerContext context)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SpecificEvent : IBaseEvent
        {
        }

        public interface IBaseEvent : IEvent
        {
        }

        public class Done : ICommand
        {
        }
    }
}