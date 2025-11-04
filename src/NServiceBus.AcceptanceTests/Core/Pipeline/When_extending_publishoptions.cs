namespace NServiceBus.AcceptanceTests.Core.Pipeline;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using Features;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_extending_publishoptions : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_make_extensions_available_to_pipeline()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Publisher>(b =>
                b.When(c => c.Subscriber1Subscribed, session =>
                {
                    var options = new PublishOptions();

                    var context = new Publisher.PublishExtensionBehavior.Context
                    {
                        Data = "ItWorks"
                    };
                    options.GetExtensions().Set(context);
                    options.GetDispatchProperties().Extensions.Add("Context", context);

                    return session.Publish(new MyEvent(), options);
                })
            )
            .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
            {
                await session.Subscribe<MyEvent>();

                if (ctx.HasNativePubSubSupport)
                {
                    ctx.Subscriber1Subscribed = true;
                }
            }))
            .Done(c => c.DataReceived is not null)
            .Run();

        Assert.That(context.DataReceived, Is.EqualTo("ItWorksItWorks"));
    }

    public class Context : ScenarioContext
    {
        public bool Subscriber1Subscribed { get; set; }

        public string DataReceived { get; set; }
    }

    public class Publisher : EndpointConfigurationBuilder
    {
        public Publisher() =>
            EndpointSetup<DefaultPublisher>(b =>
            {
                b.OnEndpointSubscribed<Context>((s, context) => { context.Subscriber1Subscribed = true; });

                b.Pipeline.Register("PublishExtensionBehavior", new PublishExtensionBehavior(), "Testing publish extensions");
            });

        public class PublishExtensionBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
        {
            public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
            {
                var myEvent = new MyEvent();

                if (context.Extensions.TryGet<Context>(out var data))
                {
                    myEvent.Data = data.Data;
                }

                if (context.Extensions.TryGet<DispatchProperties>(out var properties) &&
                    properties.Extensions.TryGetValue("Context", out var contextAsObject)
                    && contextAsObject is Context dispatchContext)
                {
                    myEvent.Data += dispatchContext.Data;
                }

                context.UpdateMessage(myEvent);

                return next(context);
            }

            public class Context
            {
                public string Data { get; set; }
            }
        }
    }

    public class Subscriber1 : EndpointConfigurationBuilder
    {
        public Subscriber1() => EndpointSetup<DefaultServer>(builder => builder.DisableFeature<AutoSubscribe>(), metadata => metadata.RegisterPublisherFor<MyEvent, Publisher>());

        public class MyHandler(Context testContext) : IHandleMessages<MyEvent>
        {
            public Task Handle(MyEvent message, IMessageHandlerContext context)
            {
                testContext.DataReceived = message.Data;
                return Task.CompletedTask;
            }
        }
    }

    public class MyEvent : IEvent
    {
        public string Data { get; set; }
    }
}