﻿namespace NServiceBus.AcceptanceTests.Hosting
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_add_host_related_headers()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<EndpointWithAuditOn>(b => b.When((bus, c) => bus.SendLocalAsync(new MessageToBeAudited())))
                    .WithEndpoint<AuditSpyEndpoint>()
                    .Done(c => c.Done)
                    .Run();

            Assert.IsNotNull(context.HostId);
            Assert.IsNotNull(context.HostName);
            Assert.IsNotNull(context.Endpoint);
            Assert.IsNotNull(context.Machine);
            Assert.IsNotNull(context.Identity);
        }

        public class Context : ScenarioContext
        {
            public bool Done { get; set; }
            public string HostId { get; set; }
            public string HostName { get; set; }
            public string Endpoint { get; set; }
            public string Machine { get; set; }
            public string Identity { get; set; }
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
                public Task Handle(MessageToBeAudited message)
                {
                    return Task.FromResult(0);
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

                public Task Handle(MessageToBeAudited message)
                {
                    Context.HostId = Bus.CurrentMessageContext.Headers[Headers.HostId];
                    Context.HostName = Bus.CurrentMessageContext.Headers[Headers.HostDisplayName];
                    Context.Endpoint = Bus.CurrentMessageContext.Headers[Headers.ProcessingEndpoint];
                    Context.Machine = Bus.CurrentMessageContext.Headers[Headers.ProcessingMachine];
                    Context.Identity = Bus.CurrentMessageContext.Headers[Headers.WindowsIdentityName];
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
        }
    }
}
