namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Features;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.MessageInterfaces;
using NUnit.Framework;
using Conventions = AcceptanceTesting.Customization.Conventions;

public class When_deserializing_interface_message_with_trimming_safe_mapper : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_move_to_error_queue_with_actionable_exception()
    {
        Context context = null;

        var exception = Assert.ThrowsAsync<MessageFailedException>(async () =>
        {
            await Scenario.Define<Context>(ctx => context = ctx)
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, ctx) => session.Publish<IMyEvent>()))
                .WithEndpoint<Subscriber>(b => b.When(async (session, ctx) =>
                {
                    await session.Subscribe<IMyEvent>();
                    if (ctx.HasNativePubSubSupport)
                    {
                        ctx.Subscribed = true;
                    }
                }))
                .Run();
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.FailedMessage.Exception, Is.TypeOf<MessageDeserializationException>());
            Assert.That(exception.FailedMessage.Exception.InnerException, Is.TypeOf<NotSupportedException>());
            Assert.That(exception.FailedMessage.Exception.InnerException!.Message, Does.Contain("dynamic code"));
            Assert.That(exception.ScenarioContext.FailedMessages, Has.Count.EqualTo(1));
            Assert.That(context!.HandlerInvoked, Is.False);
        }
    }

    public class Context : ScenarioContext
    {
        public bool Subscribed { get; set; }
        public bool HandlerInvoked { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(c =>
                {
                    c.UseSerialization<SystemJsonSerializer>();
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber))))
                        {
                            context.Subscribed = true;
                        }
                    });
                },
                metadata => metadata.RegisterSelfAsPublisherFor<IMyEvent>(this));
    }

    public class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber() =>
            EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<SystemJsonSerializer>();
                    c.DisableFeature<AutoSubscribe>();
                    // Force the trimming-safe mapper, the path NativeAOT/trimmed endpoints take.
                    c.GetSettings().Set<IMessageMapper>(new TrimmingSafeMessageMapper());
                },
                metadata => metadata.RegisterPublisherFor<IMyEvent, Publisher>());

        [Handler]
        public class MyHandler(Context testContext) : IHandleMessages<IMyEvent>
        {
            public Task Handle(IMyEvent @event, IMessageHandlerContext context)
            {
                testContext.HandlerInvoked = true;
                return Task.CompletedTask;
            }
        }
    }

    public interface IMyEvent : IEvent;
}