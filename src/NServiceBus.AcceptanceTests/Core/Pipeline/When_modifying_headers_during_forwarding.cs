namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_modifying_headers_during_forwarding : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_leak_changes_into_main_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ForwardingEndpoint>(e => e
                    .When(s => s.SendLocal(new SomeMessage())))
                .Done(c => c.HeadersInHandler != null)
                .Run();

            Assert.IsFalse(context.HeadersInHandler.ContainsKey("ForwardingHeader"));
            Assert.IsFalse(context.HeadersInPipeline.ContainsKey("ForwardingHeader"));
        }

        class Context : ScenarioContext
        {
            public IReadOnlyDictionary<string, string> HeadersInHandler { get; set; }
            public IReadOnlyDictionary<string, string> HeadersInPipeline { get; set; }
        }

        class ForwardingEndpoint : EndpointConfigurationBuilder
        {
            public ForwardingEndpoint()
            {
                EndpointSetup<DefaultServer>(e =>
                {
#pragma warning disable 618
                    e.ForwardReceivedMessagesTo("forwardingQueue");
#pragma warning restore 618
                    e.Pipeline.Register(typeof(ForwardingBehavior), "Adds headers to a forwarded message");
                    e.Pipeline.Register(typeof(MainPipelineBehavior), "Adds headers to a forwarded message");
                });
            }

            class SomeMessageHandler : IHandleMessages<SomeMessage>
            {
                Context testContext;

                public SomeMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    testContext.HeadersInHandler = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    return Task.FromResult(0);
                }
            }

            class MainPipelineBehavior : Behavior<ITransportReceiveContext>
            {
                Context testContext;

                public MainPipelineBehavior(Context testContext)
                {
                    this.testContext = testContext;
                }

                public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    await next();
                    testContext.HeadersInPipeline = new Dictionary<string, string>(context.Message.Headers);
                }
            }

#pragma warning disable 618
            class ForwardingBehavior : Behavior<IForwardingContext>
            {
                public override Task Invoke(IForwardingContext context, Func<Task> next)
                {
                    context.Message.Headers.Add("ForwardingHeader", "42");
                    return next();
                }
            }
#pragma warning restore 618
        }

        class SomeMessage
        {

        }
    }
}