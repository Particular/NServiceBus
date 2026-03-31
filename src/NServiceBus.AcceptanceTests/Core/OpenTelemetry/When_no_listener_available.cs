namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

[NonParallelizable]
public class When_no_listener_available : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_not_create_activity() =>
        Assert.DoesNotThrowAsync(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithNoListener>(b =>
                    b.When(async (session, _) => await session.SendLocal(new MyMessage())))
                .Run();
        });

    public class Context : ScenarioContext;

    public class EndpointWithNoListener : EndpointConfigurationBuilder
    {
        public EndpointWithNoListener() =>
            EndpointSetup<DefaultServer>();

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                if (Activity.Current == null)
                {
                    testContext.MarkAsCompleted();
                }
                else
                {
                    testContext.MarkAsFailed(new InvalidOperationException($"Activity should be null: {Activity.Current.DisplayName}"));
                }
                return Task.CompletedTask;
            }
        }
    }

    public class MyMessage : IMessage;
}