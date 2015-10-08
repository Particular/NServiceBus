﻿namespace NServiceBus.AcceptanceTests.Mutators
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
                .WithEndpoint<Endpoint>(b => b.When(bus => bus.SendLocalAsync(new MessageToBeMutated())))
                .Done(c => c.MessageProcessed)
                .Run();

            Assert.True(context.CanAddHeaders);
        }

        public class Context : ScenarioContext
        {
            public bool MessageProcessed { get; set; }
            public bool CanAddHeaders { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class MyTransportMessageMutator : IMutateOutgoingTransportMessages, INeedInitialization
            {
                public Task MutateOutgoing(MutateOutgoingTransportMessageContext context)
                {
                    context.OutgoingHeaders["HeaderSetByMutator"] = "some value";
                    context.OutgoingHeaders[Headers.EnclosedMessageTypes] = typeof(MessageThatMutatorChangesTo).FullName;
                    context.OutgoingBody = Encoding.UTF8.GetBytes("<MessageThatMutatorChangesTo/>");
                    return Task.FromResult(0);
                }

                public void Customize(BusConfiguration configuration)
                {
                    configuration.RegisterComponents(c => c.ConfigureComponent<MyTransportMessageMutator>(DependencyLifecycle.InstancePerCall));
                }
            }

            class MessageToBeMutatedHandler : IHandleMessages<MessageThatMutatorChangesTo>
            {
                public MessageToBeMutatedHandler(Context testContext, IBus bus)
                {
                    this.testContext = testContext;
                    this.bus = bus;
                }

                public Task Handle(MessageThatMutatorChangesTo message)
                {
                    testContext.CanAddHeaders = bus.CurrentMessageContext.Headers.ContainsKey("HeaderSetByMutator");
                    testContext.MessageProcessed = true;
                    return Task.FromResult(0);
                }

                IBus bus;
                Context testContext;
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