namespace NServiceBus.AcceptanceTests.TimeToBeReceived;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NUnit.Framework;

public class When_TimeToBeReceived_has_expired_convention : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Message_should_not_be_received() =>
        await Assert.ThatAsync(async () => await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Run(), Throws.Nothing);

    public class Context : ScenarioContext;

    class DelayReceiverFromStartingTask : FeatureStartupTask
    {
        protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            await session.SendLocal(new MyMessageWithTimeToBeReceived(), cancellationToken: cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await session.SendLocal(new MyRegularMessage(), cancellationToken: cancellationToken);
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(1);
                c.RegisterStartupTask(new DelayReceiverFromStartingTask());
                c.Conventions().DefiningTimeToBeReceivedAs(messageType => messageType == typeof(MyMessageWithTimeToBeReceived) ? TimeSpan.FromSeconds(1) : TimeSpan.MaxValue);
            });

        [Handler]
        public class MyMessageWithTimeToBeReceivedHandler(Context testContext) : IHandleMessages<MyMessageWithTimeToBeReceived>
        {
            public Task Handle(MyMessageWithTimeToBeReceived message, IMessageHandlerContext context)
            {
                testContext.MarkAsFailed(new InvalidOperationException("Should not be called"));
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class MyRegularMessageHandler(Context testContext) : IHandleMessages<MyRegularMessage>
        {
            public Task Handle(MyRegularMessage messageWithTimeToBeReceived, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessageWithTimeToBeReceived : IMessage;

    public class MyRegularMessage : IMessage;
}