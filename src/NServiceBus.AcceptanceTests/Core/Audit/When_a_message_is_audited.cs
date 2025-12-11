namespace NServiceBus.AcceptanceTests.Audit;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Audit;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_a_message_is_being_audited : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_allow_audit_action_to_be_replaced()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithSeparateBodyStorage>(b => b.When((session, c) => session.SendLocal(new MessageToBeAudited())))
            .WithEndpoint<AuditSpyEndpoint>()
            .Run();

        Assert.That(context.BodyWasEmpty, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool BodyWasEmpty { get; set; }
    }

    public class EndpointWithSeparateBodyStorage : EndpointConfigurationBuilder
    {
        public EndpointWithSeparateBodyStorage() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.AuditProcessedMessagesTo<AuditSpyEndpoint>();
                config.Pipeline.Register(typeof(AuditBodyStorageBehavior), "Simulate writing the body to a separate storage and pass a null body to the transport");
            });

        class AuditBodyStorageBehavior : Behavior<IAuditContext>
        {
            public override Task Invoke(IAuditContext context, Func<Task> next)
            {
                //body, headers and metadata can be stored separately here

                context.AuditAction = new ExcludeBodyFromAuditedMessage();
                return next();
            }

            class ExcludeBodyFromAuditedMessage : AuditAction
            {
                public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IAuditActionContext context)
                {
                    var processedMessage = context.Message;

                    //simulate the body being stored in eg. blobstorage already
                    var auditMessage = new OutgoingMessage(processedMessage.MessageId, processedMessage.Headers, ReadOnlyMemory<byte>.Empty);

                    return [context.CreateRoutingContext(auditMessage, new UnicastRoutingStrategy(context.AuditAddress))];
                }
            }
        }

        public class MessageToBeAuditedHandler : IHandleMessages<MessageToBeAudited>
        {
            public Task Handle(MessageToBeAudited message, IMessageHandlerContext context) => Task.CompletedTask;
        }
    }

    class AuditSpyEndpoint : EndpointConfigurationBuilder
    {
        public AuditSpyEndpoint() => EndpointSetup<DefaultServer, Context>((config, context) => config.Pipeline.Register(new BodySpy(context), "Detects the message being audited"));

        class BodySpy(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.BodyWasEmpty = context.Message.Body.Length == 0;
                testContext.MarkAsCompleted();
                return next();
            }
        }
    }

    public class MessageToBeAudited : IMessage
    {
    }
}