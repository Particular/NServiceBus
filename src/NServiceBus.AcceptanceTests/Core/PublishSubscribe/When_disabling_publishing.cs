namespace NServiceBus.AcceptanceTests.PublishSubscribe;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using NUnit.Framework;

public class When_disabling_publishing : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_subscribe_to_and_receive_events()
    {
        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithDisabledPublishing>(e => e.When(
                c => c.Subscribe<MyEvent>()))
            .WithEndpoint<MessageDrivenPublisher>(e => e.When(c =>
            {
                if (c.ReceivedSubscription)
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }, s => s.Publish(new MyEvent())))
            .Run();
    }

    [Test]
    public void Should_throw_when_publishing()
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<Context>()
            .WithEndpoint<EndpointWithDisabledPublishing>(e => e.When(
                c => c.Publish(new MyEvent())))
            .Done(c => c.EndpointsStarted)
            .Run());

        Assert.That(exception.Message, Does.Contain("Publishing has been explicitly disabled on this endpoint"));
    }

    public class Context : ScenarioContext
    {
        public bool ReceivedSubscription { get; set; }
    }

    public class EndpointWithDisabledPublishing : EndpointConfigurationBuilder
    {
        public EndpointWithDisabledPublishing()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
            };
            EndpointSetup(
                template,
                (c, _) =>
                {
                    // DisablePublishing API is only available on the message-driven pub/sub transport settings.
                    var routingSettings = new RoutingSettings<AcceptanceTestingTransport>(c.GetSettings());
                    routingSettings.DisablePublishing();
                },
                pm => pm.RegisterPublisherFor<MyEvent, MessageDrivenPublisher>());
        }

        [Handler]
        public class CustomHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    class MessageDrivenPublisher : EndpointConfigurationBuilder
    {
        public MessageDrivenPublisher()
        {
            var template = new DefaultServer
            {
                TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true),
                PersistenceConfiguration = new ConfigureEndpointAcceptanceTestingPersistence()
            };

            EndpointSetup(template, (c, _) => c.OnEndpointSubscribed<Context>((args, context) =>
            {
                if (args.MessageType.Contains(typeof(MyEvent).FullName))
                {
                    context.ReceivedSubscription = true;
                }
            }));
        }
    }

    public class MyEvent : IEvent;
}