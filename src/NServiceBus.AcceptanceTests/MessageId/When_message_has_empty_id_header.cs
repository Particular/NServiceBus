namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_message_has_empty_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task A_message_id_is_generated_by_the_transport_layer()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(g => g.When(b => b.SendLocal(new Message())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
            Assert.AreEqual(context.MessageId, context.Headers[Headers.MessageId], "Should populate the NServiceBus.MessageId header with the new value");
        }

        class CorruptionBehavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            public Task Invoke(IDispatchContext context, Func<IDispatchContext, CancellationToken, Task> next, CancellationToken cancellationToken)
            {
                context.Operations.First().Message.Headers[Headers.MessageId] = "";

                return next(context, cancellationToken);
            }
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string MessageId { get; set; }
            public IReadOnlyDictionary<string, string> Headers { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("CorruptionBehavior", new CorruptionBehavior(), "Corrupting the message id"));
            }

            class Handler : IHandleMessages<Message>
            {
                public Handler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(Message message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.MessageId = context.MessageId;
                    testContext.Headers = context.MessageHeaders.ToDictionary(x => x.Key, x => x.Value);
                    testContext.MessageReceived = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class Message : IMessage
        {
        }
    }
}
