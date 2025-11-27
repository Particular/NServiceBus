namespace NServiceBus.AcceptanceTests.Core.Hosting;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_scanning_is_enabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_discover_handlers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<MyEndpoint>(c => c
                .When(b => b.SendLocal(new MyMessage { SomeId = Guid.NewGuid() })))
            .Done(c => c.HandlerGotMessage && c.SagaGotMessage)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.HandlerGotMessage, Is.True);
            Assert.That(context.SagaGotMessage, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool HandlerGotMessage { get; set; }
        public bool SagaGotMessage { get; set; }
    }

    class MyEndpoint : EndpointConfigurationBuilder
    {
        public MyEndpoint() =>
            EndpointSetup<DefaultServer>()
                .DoNotAutoRegisterHandlers()
                .DoNotAutoRegisterSagas()
                .IncludeType<AutoRegisteredHandler>()
                .IncludeType<AutoRegisteredSaga>();

        public class AutoRegisteredHandler(Context testContext) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                testContext.HandlerGotMessage = true;
                return Task.CompletedTask;
            }
        }

        public class AutoRegisteredSaga(Context testContext) : Saga<AutoRegisteredSaga.AutoRegisteredSagaData>, IAmStartedByMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                Data.SomeId = message.SomeId;
                testContext.SagaGotMessage = true;
                MarkAsComplete();
                return Task.CompletedTask;
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AutoRegisteredSagaData> mapper)
                => mapper.MapSaga(s => s.SomeId).ToMessage<MyMessage>(m => m.SomeId);

            public class AutoRegisteredSagaData : ContainSagaData
            {
                public Guid SomeId { get; set; }
            }
        }
    }

    public class MyMessage : IMessage
    {
        public Guid SomeId { get; set; }
    }
}