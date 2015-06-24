namespace NServiceBus.AcceptanceTests.Hosting
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_add_host_related_headers()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<EndpointWithAuditOn>(b => b.Given(bus => bus.SendLocal(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();

            Assert.IsNotNull(context.HostId);
            Assert.IsNotNull(context.HostName);
            Assert.IsNotNull(context.Endpoint);
            Assert.IsNotNull(context.Machine);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string HostId { get; set; }
            public string HostName { get; set; }
            public string Endpoint { get; set; }
            public string Machine { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>()
                    .AuditTo<AuditSpyEndpoint>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public void Handle(MessageToBeAudited message)
                {
                }
            }
        }

        class AuditSpyEndpoint : EndpointConfigurationBuilder
        {
            public AuditSpyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public void Handle(MessageToBeAudited message)
                {
                    Context.HostId = Bus.CurrentMessageContext.Headers[Headers.HostId];
                    Context.HostName = Bus.CurrentMessageContext.Headers[Headers.HostDisplayName];
                    Context.Endpoint = Bus.CurrentMessageContext.Headers[Headers.ProcessingEndpoint];
                    Context.Machine = Bus.CurrentMessageContext.Headers[Headers.ProcessingMachine];
                    Context.Done = true;
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
