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
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithAsyncDisposable>(b =>
                {
                    b.When(e => e.SendLocal(new SomeMessage()));
                })
                .Done(c => c.WasDisposed)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool WasDisposed { get; set; }
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
                context.WasDisposed = true;
                return new ValueTask();
            }

            Context context;
        }
    }
}