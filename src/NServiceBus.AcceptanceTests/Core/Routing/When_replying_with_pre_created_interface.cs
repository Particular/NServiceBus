﻿namespace NServiceBus.AcceptanceTests.Core.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_replying_with_pre_created_interface : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_route_to_sender()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.When(b => b.SendLocal(new MyRequest())))
                .Done(c => c.GotTheReply)
                .Run();

            Assert.True(context.GotTheReply);
            Assert.AreEqual(typeof(IMyReply), context.MessageTypeInPipeline);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheReply { get; set; }
            public Type MessageTypeInPipeline { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) => c.Pipeline.Register("MessageTypeSpy", new MessageTypeSpy((Context)r.ScenarioContext), "MessageTypeSpy"));
            }

            public class StartMessageHandler : IHandleMessages<MyRequest>
            {
                public StartMessageHandler(IMessageCreator messageCreator)
                {
                    this.messageCreator = messageCreator;
                }

                public Task Handle(MyRequest message, IMessageHandlerContext context)
                {
                    var interfaceMessage = messageCreator.CreateInstance<IMyReply>();
                    return context.Reply(interfaceMessage);
                }

                IMessageCreator messageCreator;
            }

            public class IMyMessageHandler : IHandleMessages<IMyReply>
            {
                public IMyMessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(IMyReply message, IMessageHandlerContext context)
                {
                    testContext.GotTheReply = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }

            class MessageTypeSpy : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
            {
                public MessageTypeSpy(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken token)
                {
                    testContext.MessageTypeInPipeline = context.Message.MessageType;
                    return next(context, token);
                }

                Context testContext;
            }
        }

        public class MyRequest : IMessage
        {
        }

        public interface IMyReply : IMessage
        {
        }
    }
}
