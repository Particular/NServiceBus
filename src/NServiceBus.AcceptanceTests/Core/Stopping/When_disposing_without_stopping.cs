namespace NServiceBus.AcceptanceTests.Core.Stopping;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_disposing_without_stopping : NServiceBusAcceptanceTest
{
    [Test]
    [CancelAfter(30000)]
    public async Task Should_initiate_immediate_handler_cancellation(CancellationToken cancellationToken = default) =>
        await Scenario.Define<Context>()
            .WithEndpoint<EndpointThatGetsRugPulled>(b =>
                b.ServiceResolve(static async (provider, context, token) =>
                {
                    var session = provider.GetRequiredService<IMessageSession>();
                    await session.SendLocal(new MessageThatTakesALongTime(), token);

                    await context.MessageReceived.Task.WaitAsync(token);

                    var disposer = provider.GetRequiredKeyedService<Func<ValueTask>>("Disposer");
                    await disposer();
                }, true))
            .Run(cancellationToken);

    public class Context : ScenarioContext
    {
        public TaskCompletionSource MessageReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class EndpointThatGetsRugPulled : EndpointConfigurationBuilder
    {
        public EndpointThatGetsRugPulled() => EndpointSetup<DefaultServer>();

        [Handler]
        public class InfiniteHandler(Context testContext) : IHandleMessages<MessageThatTakesALongTime>
        {
            public async Task Handle(MessageThatTakesALongTime message, IMessageHandlerContext context)
            {
                try
                {
                    testContext.MessageReceived.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                    testContext.MarkAsCompleted();
                }
            }
        }
    }

    public class MessageThatTakesALongTime : IMessage;
}