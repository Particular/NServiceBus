namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

/// <summary>
/// This test deliberately uses a bunch of weird keyed service registrations to verify that many key types just work as expected.
/// Async disposables are used to also demonstrate the disposal behavior when the service provider is externally managed.
/// </summary>
[TestFixture]
public class When_registering_keyed_async_disposables_externally_managed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispose()
    {
        var context = await Scenario.Define<Context>()
            .WithServices(static services =>
            {
                services.AddKeyedSingleton<SingletonAsyncDisposableShared>(false);
                services.AddKeyedScoped<ScopedAsyncDisposableShared>(true);
            })
            .WithEndpoint<EndpointWithAsyncDisposable>(b =>
            {
                b.Services(static s =>
                {
                    s.AddKeyedSingleton<SingletonAsyncDisposable>(256);
                    s.AddKeyedScoped<ScopedAsyncDisposable>("scoped-async-disposable");
                })
                .When(e => e.SendLocal(new SomeMessage()));
            })
            .Run();

        // the acceptance test infrastructure disposes the managed provider

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ScopedAsyncDisposableDisposed, Is.True, "Scoped AsyncDisposable wasn't disposed as it should have been.");
            Assert.That(context.SingletonAsyncDisposableDisposed, Is.True, "Singleton AsyncDisposable wasn't disposed as it should have been.");
            Assert.That(context.SingletonAsyncDisposableSharedDisposed, Is.True, "Singleton AsyncDisposable Shared wasn't disposed as it should have been.");
            Assert.That(context.ScopedAsyncDisposableSharedDisposed, Is.True, "Scoped AsyncDisposable Shared wasn't disposed as it should have been.");
        }
    }

    class Context : ScenarioContext
    {
        public bool ScopedAsyncDisposableDisposed { get; set; }
        public bool SingletonAsyncDisposableDisposed { get; set; }
        public bool SingletonAsyncDisposableSharedDisposed { get; set; }
        public bool ScopedAsyncDisposableSharedDisposed { get; set; }
    }

    public class EndpointWithAsyncDisposable : EndpointConfigurationBuilder
    {
        public EndpointWithAsyncDisposable() =>
            EndpointSetup<DefaultServer>();

        class HandlerWithAsyncDisposable(
            Context testContext,
            [FromKeyedServices("scoped-async-disposable")] ScopedAsyncDisposable scopedAsyncDisposable,
            [FromKeyedServices(256)] SingletonAsyncDisposable singletonAsyncDisposable,
            [FromKeyedServices(false)] SingletonAsyncDisposableShared singletonAsyncDisposableShared,
            [FromKeyedServices(true)] ScopedAsyncDisposableShared scopedAsyncDisposableShared,
            IServiceProvider serviceProvider)
            : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                scopedAsyncDisposable.Initialize(testContext);
                singletonAsyncDisposable.Initialize(testContext);
                singletonAsyncDisposableShared.Initialize(testContext);
                scopedAsyncDisposableShared.Initialize(testContext);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(scopedAsyncDisposable, Is.SameAs(serviceProvider.GetRequiredKeyedService<ScopedAsyncDisposable>("scoped-async-disposable")));
                    Assert.That(scopedAsyncDisposable, Is.SameAs(serviceProvider.GetKeyedService<ScopedAsyncDisposable>("scoped-async-disposable")));

                    Assert.That(singletonAsyncDisposable, Is.SameAs(serviceProvider.GetRequiredKeyedService<SingletonAsyncDisposable>(256)));
                    Assert.That(singletonAsyncDisposable, Is.SameAs(serviceProvider.GetKeyedService<SingletonAsyncDisposable>(256)));

                    Assert.That(scopedAsyncDisposableShared, Is.SameAs(serviceProvider.GetRequiredKeyedService<ScopedAsyncDisposableShared>(true)));
                    Assert.That(scopedAsyncDisposableShared, Is.SameAs(serviceProvider.GetKeyedService<ScopedAsyncDisposableShared>(true)));

                    Assert.That(singletonAsyncDisposableShared, Is.SameAs(serviceProvider.GetRequiredKeyedService<SingletonAsyncDisposableShared>(false)));
                    Assert.That(singletonAsyncDisposableShared, Is.SameAs(serviceProvider.GetKeyedService<SingletonAsyncDisposableShared>(false)));
                }
                return Task.CompletedTask;
            }
        }
    }

    public class SomeMessage : IMessage;

    abstract class InitializableBase : IAsyncDisposable
    {
        // This method is here to make the code being used in the handler to not trigger compiler warnings
        public void Initialize(Context scenarioContext) => context = scenarioContext;

        public abstract ValueTask DisposeAsync();

        protected Context context;
    }

    class SingletonAsyncDisposableShared : InitializableBase
    {
        public override ValueTask DisposeAsync()
        {
            context.SingletonAsyncDisposableSharedDisposed = true;
            return new ValueTask();
        }
    }

    class ScopedAsyncDisposableShared : InitializableBase
    {
        public override ValueTask DisposeAsync()
        {
            context.ScopedAsyncDisposableSharedDisposed = true;
            return new ValueTask();
        }
    }

    class SingletonAsyncDisposable : InitializableBase
    {
        public override ValueTask DisposeAsync()
        {
            context.SingletonAsyncDisposableDisposed = true;
            return new ValueTask();
        }
    }

    sealed class ScopedAsyncDisposable : InitializableBase
    {
        public override ValueTask DisposeAsync()
        {
            context.ScopedAsyncDisposableDisposed = true;
            context.MarkAsCompleted();
            return new ValueTask();
        }
    }
}