namespace NServiceBus.AcceptanceTests.Core.Mutators
{
    using System.Text;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageMutator;
    using NUnit.Framework;

    public class When_using_outgoing_tm_mutator : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_update_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(session => session.SendLocal(new MessageToBeMutated())))
                .Done(c => c.MessageProcessed)
                .Run();

            Assert.True(context.CanAddHeaders);
            Assert.AreEqual("SomeValue", context.MutatedPropertyValue, "Mutator should be able to mutate body.");
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool CanAddHeaders { get; set; }
            public string MutatedPropertyValue { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseSerialization<XmlSerializer>();
                    c.RegisterMessageMutator(new MyTransportMessageMutator());
                });
            }

            class MyTransportMessageMutator : IMutateOutgoingTransportMessages
            {
                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingHeaders["HeaderSetByMutator"] = "some value";
                    context.OutgoingHeaders[Headers.EnclosedMessageTypes] = typeof(MessageThatMutatorChangesTo).FullName;
                    context.OutgoingBody = Encoding.UTF8.GetBytes("<MessageThatMutatorChangesTo><SomeProperty>SomeValue</SomeProperty></MessageThatMutatorChangesTo>");
                    return Task.FromResult(0);
                }
            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageThatMutatorChangesTo>
            {
                public MessageToBeMutatedHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(MessageThatMutatorChangesTo message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.CanAddHeaders = context.MessageHeaders.ContainsKey("HeaderSetByMutator");
                    testContext.MutatedPropertyValue = message.SomeProperty;
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MessageToBeMutated : ICommand
        {
        }

        public class MessageThatMutatorChangesTo : ICommand
        {
            public string SomeProperty { get; set; }
        }
    }
}