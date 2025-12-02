namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class When_registering_keyed_services_externally_managed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispose()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAsyncDisposable>(b =>
            {
                b.Services(static s =>
                {
                    s.AddKeyedSingleton<SingletonAsyncDisposable>("singleton-async-disposable");
                    s.AddKeyedScoped<ScopedAsyncDisposable>("scoped-async-disposable");
                })
                .When(e => e.SendLocal(new SomeMessage()));
            })
            .Done(c => c.ScopedAsyncDisposableDisposed)
            .Run(TimeSpan.FromSeconds(10));

        // the acceptance test infrastructure disposes the managed provider

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ScopedAsyncDisposableDisposed, Is.True, "Scoped AsyncDisposable wasn't disposed as it should have been.");
            Assert.That(context.SingletonAsyncDisposableDisposed, Is.True, "Singleton AsyncDisposable wasn't disposed as it should have been.");
        }
    }

    class Context : ScenarioContext
    {
        public bool ScopedAsyncDisposableDisposed { get; set; }
        public bool SingletonAsyncDisposableDisposed { get; set; }
    }

    public class EndpointWithAsyncDisposable : EndpointConfigurationBuilder
    {
        public EndpointWithAsyncDisposable() =>
            EndpointSetup<DefaultServer>();

        class HandlerWithAsyncDisposable(
            Context testContext,
            [FromKeyedServices("scoped-async-disposable")] ScopedAsyncDisposable scopedAsyncDisposable,
            [FromKeyedServices("singleton-async-disposable")] SingletonAsyncDisposable singletonAsyncDisposable)
            : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                scopedAsyncDisposable.Initialize(testContext);
                singletonAsyncDisposable.Initialize(testContext);
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;

    class SingletonAsyncDisposable : IAsyncDisposable
    {
        // This method is here to make the code being used in the handler to not trigger compiler warnings
        public void Initialize(Context scenarioContext) => context = scenarioContext;

        public ValueTask DisposeAsync()
        {
            context.SingletonAsyncDisposableDisposed = true;
            return new ValueTask();
        }

        Context context;
    }

    class ScopedAsyncDisposable : IAsyncDisposable
    {
        // This method is here to make the code being used in the handler to not trigger compiler warnings
        public void Initialize(Context scenarioContext) => context = scenarioContext;

        public ValueTask DisposeAsync()
        {
            context.ScopedAsyncDisposableDisposed = true;
            return new ValueTask();
        }

        Context context;
    }
}