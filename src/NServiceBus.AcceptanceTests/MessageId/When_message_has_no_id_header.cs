namespace NServiceBus.AcceptanceTests.MessageId
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Pipeline;
    using NUnit.Framework;

    public class When_message_has_no_id_header : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task A_message_id_is_generated_by_the_transport_layer_on_receiving_side()
        {
            var context = await Scenario.Define<Context>()
                     .WithEndpoint<Endpoint>(g => g.When(async b => await b.SendLocal(new Message())))
                     .Done(c => c.MessageReceived)
                     .Run();

            Assert.IsFalse(string.IsNullOrWhiteSpace(context.MessageId));
        }

        public class CorruptionBehavior : Behavior<IDispatchContext>
        {
            public Context Context { get; set; }

            public override Task Invoke(IDispatchContext context, Func<Task> next)
            {
                context.Operations.First().Message.Headers[Headers.MessageId] = null;

                return next();
            }
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public string MessageId { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("CorruptionBehavior", typeof(When_message_has_empty_id_header.CorruptionBehavior), "Corrupting the message id"));
            }

            class Handler : IHandleMessages<Message>
            {
                public Context TestContext { get; set; }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    TestContext.MessageId = context.MessageId;
                    TestContext.MessageReceived = true;

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