namespace NServiceBus.AcceptanceTests.PushPipelines;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using Features;
using NUnit.Framework;
using Transport;

public class When_message_is_pushed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_receive_the_message()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(async (_, c) =>
            {
                try
                {
                    await c.Pipeline.Handle(new MessageContext("SomeId",
                        new Dictionary<string, string> { { Headers.EnclosedMessageTypes, typeof(MyMessage).FullName } },
                        "{}"u8.ToArray(), new TransportTransaction(), "MyPushPipeline", new ContextBag()));
                }
                catch (Exception e)
                {
                    var handleResult = await c.Pipeline.Handle(new ErrorContext(e,
                        new Dictionary<string, string> { { Headers.EnclosedMessageTypes, typeof(MyMessage).FullName } },
                        "SomeId", "{}"u8.ToArray(), new TransportTransaction(), 0, "MyPushPipeline", new ContextBag()));

                    Assert.That(handleResult, Is.EqualTo(ErrorHandleResult.RetryRequired));

                    await c.Pipeline.Handle(new MessageContext("SomeId",
                        new Dictionary<string, string> { { Headers.EnclosedMessageTypes, typeof(MyMessage).FullName } },
                        "{}"u8.ToArray(), new TransportTransaction(), "MyPushPipeline", new ContextBag()));
                }
            }))
            .Done(c => c.MessageReceived)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.MessageReceived, Is.True);
        });
    }

    class Context : ScenarioContext
    {
        public bool FirstTime { get; set; } = true;
        public bool MessageReceived { get; set; }
        public IPushPipeline Pipeline { get; set; }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() => EndpointSetup<DefaultServer>();

        public class PushPipelineFeature : Feature
        {
            public PushPipelineFeature() => EnableByDefault();

            protected override void Setup(FeatureConfigurationContext context)
            {
                var pushPipeline = context.AddPushPipeline("MyPushPipeline");

                var testContext = (Context)context.Settings.Get<ScenarioContext>();
                testContext.Pipeline = pushPipeline;
            }
        }

        class MyHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (testContext.FirstTime)
                {
                    testContext.FirstTime = false;
                    throw new Exception("Simulated exception");
                }

                testContext.MessageReceived = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}