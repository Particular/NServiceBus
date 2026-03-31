namespace NServiceBus.AcceptanceTests.Core.Routing;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_sending_from_outgoing_pipeline : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(15_000)]
    public async Task Should_use_default_routing_when_empty_send_options(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointA>(e => e
                .CustomConfig(c =>
                {
                    c.Pipeline.Register(new SendingBehaviorUsingDefaultRouting(), "outgoing pipeline behavior sending messages");
                    c.ConfigureRouting().RouteToEndpoint(typeof(BehaviorMessage), typeof(EndpointB));
                })
                .When(s => s.SendLocal(new LocalMessage())))
            .WithEndpoint<EndpointB>()
            .Run(cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.LocalMessageReceived, Is.True);
            Assert.That(context.BehaviorMessageReceived, Is.True);
        }
    }

    [Test, CancelAfter(15_000)]
    public async Task Should_apply_send_options_routing(CancellationToken cancellationToken = default)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointA>(e => e
                .CustomConfig(c =>
                {
                    c.Pipeline.Register(new SendingBehaviorUsingRoutingOverride(), "outgoing pipeline behavior sending messages");
                })
                .When(s => s.SendLocal(new LocalMessage())))
            .WithEndpoint<EndpointB>()
            .Run(cancellationToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.LocalMessageReceived, Is.True);
            Assert.That(context.BehaviorMessageReceived, Is.True);
        }
    }

    public class Context : ScenarioContext
    {
        public bool BehaviorMessageReceived { get; set; }
        public bool LocalMessageReceived { get; set; }

        public void MaybeCompleted() => MarkAsCompleted(LocalMessageReceived, BehaviorMessageReceived);
    }

    public class EndpointA : EndpointConfigurationBuilder
    {
        public EndpointA() => EndpointSetup<DefaultServer>();

        [Handler]
        public class LocalMessageHandler(Context testContext) : IHandleMessages<LocalMessage>
        {
            public Task Handle(LocalMessage message, IMessageHandlerContext context)
            {
                testContext.LocalMessageReceived = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class EndpointB : EndpointConfigurationBuilder
    {
        public EndpointB() => EndpointSetup<DefaultServer>();

        [Handler]
        public class BehaviorMessageHandler(Context testContext) : IHandleMessages<BehaviorMessage>
        {
            public Task Handle(BehaviorMessage message, IMessageHandlerContext context)
            {
                testContext.BehaviorMessageReceived = true;
                testContext.MaybeCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class SendingBehaviorUsingDefaultRouting : Behavior<IOutgoingLogicalMessageContext>
    {
        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            await next();
            if (context.Message.MessageType != typeof(BehaviorMessage)) // prevent infinite loop
            {
                await context.Send(new BehaviorMessage(), new SendOptions()); // use empty SendOptions
            }
        }
    }

    public class SendingBehaviorUsingRoutingOverride : Behavior<IOutgoingLogicalMessageContext>
    {
        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            await next();
            if (context.Message.MessageType != typeof(BehaviorMessage)) // prevent infinite loop
            {
                var sendOptions = new SendOptions();
                sendOptions.SetDestination(Conventions.EndpointNamingConvention(typeof(EndpointB))); // Configure routing on SendOptions
                await context.Send(new BehaviorMessage(), sendOptions);
            }
        }
    }

    public class LocalMessage : IMessage;

    public class BehaviorMessage : IMessage;
}