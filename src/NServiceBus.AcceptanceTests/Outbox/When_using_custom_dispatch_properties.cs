namespace NServiceBus.AcceptanceTests.Outbox;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using NServiceBus.Pipeline;
using NUnit.Framework;
using Transport;

public class When_using_custom_dispatch_properties : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_preserve_them_on_failure()
    {
        Requires.OutboxPersistence();

        var context = await Scenario.Define<Context>()
            .WithEndpoint<NonDtcReceivingEndpoint>(b => b
                .DoNotFailOnErrorMessages()
                .When(session => session.SendLocal(new KickoffMessage())))
            .Run();

        Assert.That(context.DispatchProperties, Does.ContainKey("CustomDispatchProperty").WithValue("CustomValue"), "Dispatch property wasn't loaded from outbox records");
    }

    public class Context : ScenarioContext
    {
        public bool Throw { get; set; } = true;

        public DispatchProperties DispatchProperties { get; set; }
    }

    public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
    {
        public NonDtcReceivingEndpoint() =>
            EndpointSetup<DefaultServer>((b, r) =>
            {
                b.Recoverability().Immediate(c => c.NumberOfRetries(2));
                b.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                b.EnableOutbox();
                b.Pipeline.Register("BlowUpBeforeDispatchBehavior", new BlowUpBeforeDispatchBehavior((Context)r.ScenarioContext), "For testing");
            });

        class BlowUpBeforeDispatchBehavior(Context testContext) : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
        {
            public async Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
            {
                if (testContext.Throw)
                {
                    testContext.Throw = false;
                    // When blowing up here, the outbox records have already been stored,
                    // the message gets retried, and the outbox operations loaded again
                    throw new SimulatedException();
                }

                // Here we capture the dispatch properties that have been loaded from the outbox records.
                testContext.DispatchProperties = context.Operations.Single().Properties;
                await next(context).ConfigureAwait(false);
            }
        }

        [Handler]
        public class KickoffHandler : IHandleMessages<KickoffMessage>
        {
            public Task Handle(KickoffMessage message, IMessageHandlerContext context)
            {
                var sendOptions = new SendOptions();
                sendOptions.RouteToThisEndpoint();
                var properties = sendOptions.GetDispatchProperties();
                properties.Add("CustomDispatchProperty", "CustomValue");
                return context.Send(new FollowUpMessage(), sendOptions);
            }
        }

        [Handler]
        public class FollowUpHandler(Context testContext) : IHandleMessages<FollowUpMessage>
        {
            public Task Handle(FollowUpMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class KickoffMessage : ICommand;

    public class FollowUpMessage : IMessage;
}