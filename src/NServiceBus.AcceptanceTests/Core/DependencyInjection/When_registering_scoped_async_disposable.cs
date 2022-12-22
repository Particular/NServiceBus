namespace NServiceBus.AcceptanceTests.Core.DependencyInjection
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    [TestFixture]
    public class When_registering_scoped_async_disposable : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_dispose()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAsyncDisposable>(b =>
                {
                    b.When(e => e.SendLocal(new SomeMessage()));
                })
                .Done(c => c.AsyncDisposableDisposed)
                .Run();

            Assert.That(context.AsyncDisposableDisposed, Is.True, "AsyncDisposable wasn't disposed as it should have been.");
        }

        class Context : ScenarioContext
        {
            public bool AsyncDisposableDisposed { get; set; }
        }

        public class EndpointWithAsyncDisposable : EndpointConfigurationBuilder
        {
            public EndpointWithAsyncDisposable() =>
                EndpointSetup<DefaultServer>(c => c.RegisterComponents(s => s.AddScoped<AsyncDisposable>()));

            class HandlerWithAsyncDisposable : IHandleMessages<SomeMessage>
            {
                public HandlerWithAsyncDisposable(Context context, AsyncDisposable asyncDisposable)
                {
                    testContext = context;
                    this.asyncDisposable = asyncDisposable;
                }

                public Task Handle(SomeMessage message, IMessageHandlerContext context)
                {
                    asyncDisposable.Initialize(testContext);
                    return Task.CompletedTask;
                }

                Context testContext;
                readonly AsyncDisposable asyncDisposable;
            }
        }

        public class SomeMessage : IMessage
        {
        }

        class AsyncDisposable : IAsyncDisposable
        {
            public void Initialize(Context scenarioContext) => context = scenarioContext;

            public ValueTask DisposeAsync()
            {
                context.AsyncDisposableDisposed = true;
                return new ValueTask();
            }

            Context context;
        }
    }
}