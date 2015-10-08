namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Pipeline;
    using NUnit.Framework;

    public class When_message_has_empty_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task A_message_id_is_generated_by_the_transport_layer()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(g => g.When(async b => await b.SendLocalAsync(new Message())))
                    .Done(c => c.MessageReceived)
                    .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
            Assert.AreEqual(context.MessageId, context.Headers[Headers.MessageId], "Should populate the NServiceBus.MessageId header with the new value");
        }

        public class CorruptionBehavior : Behavior<DispatchContext>
        {
            public Context Context { get; set; }

            public override Task Invoke(DispatchContext context, Func<Task> next)
            {
                context.Operations.First().Message.Headers[Headers.MessageId] = "";

                return next();
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string MessageId { get; set; }
            public IDictionary<string, string> Headers { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("CorruptionBehavior", typeof(CorruptionBehavior), "Corrupting the message id"));
            }

            class Handler : IHandleMessages<Message>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(Message message)
                {
                    Context.MessageId = Bus.CurrentMessageContext.Id;
                    Context.Headers = Bus.CurrentMessageContext.Headers;
                    Context.MessageReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class Message : IMessage
        {
        }
    }

}