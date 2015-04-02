namespace NServiceBus.AcceptanceTests.Audit
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_using_audit_message_is_received : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_contain_correct_headers()
        {
            var context = new Context();
            Scenario.Define(context)
            .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<EndpointThatHandlesAuditMessages>()
            .Done(c => c.IsMessageHandlingComplete && context.IsMessageHandledByTheAuditEndpoint)
            .Run();
            Assert.IsTrue(context.Headers.ContainsKey(Headers.ProcessingStarted));
            Assert.IsTrue(context.Headers.ContainsKey(Headers.ProcessingEnded));
            Assert.IsTrue(context.IsMessageHandledByTheAuditEndpoint);
        }

        public class Context : ScenarioContext
        {
            public bool IsMessageHandlingComplete { get; set; }
            public bool IsMessageHandledByTheAuditEndpoint { get; set; }
            public IDictionary<string,string> Headers{ get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {

            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<EndpointThatHandlesAuditMessages>();
            }

            class MessageToBeAuditedHandler : IProcessCommands<MessageToBeAudited>
            {
                Context context;

                public MessageToBeAuditedHandler(Context context)
                {
                    this.context = context;
                }

                public void Handle(MessageToBeAudited message, ICommandContext context)
                {
                    this.context.IsMessageHandlingComplete = true;
                }
            }
        }

        public class EndpointThatHandlesAuditMessages : EndpointConfigurationBuilder
        {

            public EndpointThatHandlesAuditMessages()
            {
                EndpointSetup<DefaultServer>();
            }

            class AuditMessageHandler : IProcessCommands<MessageToBeAudited>
            {
                Context context;
                IBus bus;

                public AuditMessageHandler(Context context, IBus bus)
                {
                    this.context = context;
                    this.bus = bus;
                }

                public void Handle(MessageToBeAudited message, ICommandContext context)
                {
                    // TODO: Design a good API to access headers
                    this.context.IsMessageHandledByTheAuditEndpoint = true;
                    this.context.Headers = bus.CurrentMessageContext.Headers;
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : ICommand
        {
        }
    }
}
