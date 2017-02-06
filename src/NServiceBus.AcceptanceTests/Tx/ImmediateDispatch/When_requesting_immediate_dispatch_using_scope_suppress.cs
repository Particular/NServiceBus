﻿namespace NServiceBus.AcceptanceTests.Tx
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Logging;
    using NUnit.Framework;

    //note: this test will no longer be relevant in v7
    public class When_requesting_immediate_dispatch_using_scope_suppress : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispatch_immediately()
        {
            Requires.DtcSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<SuppressEndpoint>(b => b
                    .When(session => session.SendLocal(new InitiatingMessage()))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.MessageDispatched)
                .Run();

            Assert.True(context.MessageDispatched, "Should dispatch the message immediately");
            Assert.True(context.Logs.Any(l => l.Level == LogLevel.Warn && l.Message.Contains("Suppressed ambient transaction detected when requesting the outgoing operation")));
        }

        public class Context : ScenarioContext
        {
            public bool MessageDispatched { get; set; }
        }

        public class SuppressEndpoint : EndpointConfigurationBuilder
        {
            public SuppressEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class InitiatingMessageHandler : IHandleMessages<InitiatingMessage>
            {
                public async Task Handle(InitiatingMessage message, IMessageHandlerContext context)
                {
                    using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await context.SendLocal(new MessageToBeDispatchedImmediately());
                    }

                    throw new SimulatedException();
                }
            }

            public class MessageToBeDispatchedImmediatelyHandler : IHandleMessages<MessageToBeDispatchedImmediately>
            {
                public Context Context { get; set; }

                public Task Handle(MessageToBeDispatchedImmediately message, IMessageHandlerContext context)
                {
                    Context.MessageDispatched = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class InitiatingMessage : ICommand
        {
        }

        public class MessageToBeDispatchedImmediately : ICommand
        {
        }
    }
}