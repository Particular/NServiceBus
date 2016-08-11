﻿namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_a_message_is_audited : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_be_dispatched_immediately()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAuditOn>(b => b
                    .When(session => session.SendLocal(new MessageToBeAudited()))
                    .DoNotFailOnErrorMessages())
                .WithEndpoint<AuditSpyEndpoint>()
                .Done(c => c.MessageAudited)
                .Repeat(r => r.For<AllOutboxCapableStorages>())
                .Should(c => Assert.True(c.MessageAudited))
                .Run();
        }

        public class Context : ScenarioContext
        {
            public bool MessageAudited { get; set; }
        }

        public class EndpointWithAuditOn : EndpointConfigurationBuilder
        {
            public EndpointWithAuditOn()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableOutbox();
                        b.Pipeline.Register("BlowUpAfterDispatchBehavior", new BlowUpAfterDispatchBehavior(), "For testing");
                    })
                    .AuditTo<AuditSpyEndpoint>();
            }

            class BlowUpAfterDispatchBehavior : Behavior<IBatchDispatchContext>
            {
                public override async Task Invoke(IBatchDispatchContext context, Func<Task> next)
                {
                    if (!context.Operations.Any(op => op.Message.Headers[Headers.EnclosedMessageTypes].Contains(typeof(MessageToBeAudited).Name)))
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    await next().ConfigureAwait(false);

                    throw new SimulatedException();
                }
            }

            public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
            {
                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
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

                public Task Handle(MessageToBeAudited message, IMessageHandlerContext context)
                {
                    Context.MessageAudited = true;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MessageToBeAudited : IMessage
        {
            public string RunId { get; set; }
        }
    }
}