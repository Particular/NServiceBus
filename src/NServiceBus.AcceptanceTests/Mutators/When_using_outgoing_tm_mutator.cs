namespace NServiceBus.AcceptanceTests.Mutators
{
    using System.Text;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;

    public class When_using_outgoing_tm_mutator : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_update_message()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<OutgoingTMMutatorEndpoint>(b => b.Given(bus => bus.SendLocal(new MessageToBeMutated())))
                    .Done(c => c.MessageProcessed)
                    .Run();

            Assert.True(context.CanAddHeaders);
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool CanAddHeaders { get; set; }
        }

        public class OutgoingTMMutatorEndpoint : EndpointConfigurationBuilder
        {
            public OutgoingTMMutatorEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyTransportMessageMutator : IMutateOutgoingTransportMessages, INeedInitialization
            {

                public Context Context { get; set; }

                public void MutateOutgoing(MutateOutgoingTransportMessagesContext context)
                {
                    context.SetHeader("HeaderSetByMutator", "some value");

                    context.SetHeader(Headers.EnclosedMessageTypes, typeof(MessageThatMutatorChangesTo).FullName);
                    context.Body = Encoding.UTF8.GetBytes("<MessageThatMutatorChangesTo/>");
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageThatMutatorChangesTo>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }


                public void Handle(MessageThatMutatorChangesTo message)
                {
                    Context.CanAddHeaders = Bus.CurrentMessageContext.Headers.ContainsKey("HeaderSetByMutator");
                    Context.MessageProcessed = true;
                }
            }

        }

        public class MessageToBeMutated : ICommand
        {
        }

        public class MessageThatMutatorChangesTo : ICommand
        {
        }
    }
}