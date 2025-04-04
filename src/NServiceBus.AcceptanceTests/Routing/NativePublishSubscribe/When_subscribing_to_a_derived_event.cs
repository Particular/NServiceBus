﻿namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe;

using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_subscribing_to_a_derived_event : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Base_event_should_not_be_delivered()
    {
        Requires.NativePubSubSupport();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
            {
                await session.Publish<IBaseEvent>();
                await session.Send(new Done());
            }))
            .WithEndpoint<Subscriber>(b => b.When(async (session, c) =>
            {
                await session.Subscribe<SpecificEvent>();
                c.SubscriberSubscribed = true;
            }))
            .Done(c => c.Done)
            .Run();

        Assert.That(context.SubscriberGotEvent, Is.False);
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
            EndpointSetup<DefaultPublisher>(b =>
                b.ConfigureRouting().RouteToEndpoint(typeof(Done), typeof(Subscriber)),
                metadata => metadata.RegisterSelfAsPublisherFor<IBaseEvent>(this));
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
            }, metadata => metadata.RegisterPublisherFor<SpecificEvent, Publisher>());
        }

        public class MyHandler : IHandleMessages<SpecificEvent>
        {
            public MyHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(SpecificEvent messageThatIsEnlisted, IMessageHandlerContext context)
            {
                testContext.SubscriberGotEvent = true;
                return Task.CompletedTask;
            }

            Context testContext;
        }

        public class DoneHandler : IHandleMessages<Done>
        {
            public DoneHandler(Context context)
            {
                testContext = context;
            }

            public Task Handle(Done message, IMessageHandlerContext context)
            {
                testContext.Done = true;
                return Task.CompletedTask;
            }

            Context testContext;
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