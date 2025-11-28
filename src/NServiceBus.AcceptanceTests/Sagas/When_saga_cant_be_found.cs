namespace NServiceBus.AcceptanceTests.Sagas;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_saga_cant_be_found : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_invoke_all_not_found_handlers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<SagaEndpoint>(b => b.When(session => session.SendLocal(new MessageToSaga { Id = Guid.NewGuid() })))
            .Done(c => c.NotFoundHandlerCalled)
            .Run();

        Assert.That(context.NotFoundHandlerCalled, Is.True);
        Assert.That(context.TimesDuplicatedHandlerCalled, Is.EqualTo(1));
        Assert.That(context.AsyncDisposableCalled, Is.True);
        Assert.That(context.DisposableCalled, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool NotFoundHandlerCalled { get; set; }
        public int TimesDuplicatedHandlerCalled { get; set; }
        public bool AsyncDisposableCalled { get; set; }
        public bool DisposableCalled { get; set; }
    }

    public class SagaEndpoint : EndpointConfigurationBuilder
    {
        public SagaEndpoint() => EndpointSetup<DefaultServer>(c => c.AddSaga<CantBeFoundSaga>());

        public class CantBeFoundSaga : Saga<CantBeFoundSaga.CantBeFoundSagaData>, IAmStartedByMessages<StartSaga>, IHandleMessages<MessageToSaga>
        {
            public Task Handle(StartSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            public Task Handle(MessageToSaga message, IMessageHandlerContext context) => Task.CompletedTask;

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CantBeFoundSagaData> mapper)
            {
                mapper.MapSaga(s => s.MessageId)
                    .ToMessage<StartSaga>(m => m.Id)
                    .ToMessage<MessageToSaga>(m => m.Id);

                mapper.ConfigureNotFoundHandler<NotFoundHandler>();
                mapper.ConfigureNotFoundHandler<DuplicateRegistrationHandler>();
                mapper.ConfigureNotFoundHandler<DuplicateRegistrationHandler>();
                mapper.ConfigureNotFoundHandler<AsyncDisposableHandler>();
                mapper.ConfigureNotFoundHandler<DisposableHandler>();
            }

            public class CantBeFoundSagaData : ContainSagaData
            {
                public virtual Guid MessageId { get; set; }
            }

            public class NotFoundHandler(Context testContext) : IHandleSagaNotFound
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    testContext.NotFoundHandlerCalled = true;
                    return Task.CompletedTask;
                }
            }

            public class DuplicateRegistrationHandler(Context testContext) : IHandleSagaNotFound
            {
                public Task Handle(object message, IMessageProcessingContext context)
                {
                    testContext.TimesDuplicatedHandlerCalled++;
                    return Task.CompletedTask;
                }
            }

            public class AsyncDisposableHandler(Context testContext) : IHandleSagaNotFound, IAsyncDisposable
            {
                public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;

#pragma warning disable CA1816
                public ValueTask DisposeAsync()
#pragma warning restore CA1816
                {
                    testContext.AsyncDisposableCalled = true;
                    return ValueTask.CompletedTask;
                }
            }

            public class DisposableHandler(Context testContext) : IHandleSagaNotFound, IDisposable
            {
                public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;

#pragma warning disable CA1816
                public void Dispose() => testContext.DisposableCalled = true;
#pragma warning restore CA1816
            }
        }
    }

    public class StartSaga : ICommand
    {
        public Guid Id { get; set; }
    }

    public class MessageToSaga : ICommand
    {
        public Guid Id { get; set; }
    }
}