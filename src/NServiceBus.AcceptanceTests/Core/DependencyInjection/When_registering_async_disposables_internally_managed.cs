namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class When_registering_async_disposables_internally_managed : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_dispose()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithAsyncDisposable>(b =>
            {
                b.ToCreateInstance(
                    (_, configuration) => Endpoint.Create(configuration),
                    (startableEndpoint, _, ct) => startableEndpoint.Start(ct));
                b.When(e => e.SendLocal(new SomeMessage()));
            })
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ScopedAsyncDisposableDisposed, Is.True, "Scoped AsyncDisposable wasn't disposed as it should have been.");
            Assert.That(context.SingletonAsyncDisposableDisposed, Is.True, "Singleton AsyncDisposable wasn't disposed as it should have been.");
        }
    }

    public class Context : ScenarioContext
    {
        public bool ScopedAsyncDisposableDisposed { get; set; }
        public bool SingletonAsyncDisposableDisposed { get; set; }
    }

    public class EndpointWithAsyncDisposable : EndpointConfigurationBuilder
    {
        public EndpointWithAsyncDisposable() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.RegisterComponents(s =>
                {
                    // We have to take control over re-registering the context because we have taken control over the instance creation
                    s.AddSingleton(c.GetSettings().Get<Context>());
                    s.AddScoped<ScopedAsyncDisposable>();
                    s.AddSingleton<SingletonAsyncDisposable>();
                });
            });

        [Handler]
        public class HandlerWithAsyncDisposable(
            Context testContext,
            ScopedAsyncDisposable scopedAsyncDisposable,
            SingletonAsyncDisposable singletonAsyncDisposable)
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

    public sealed class SingletonAsyncDisposable : IAsyncDisposable
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

    public sealed class ScopedAsyncDisposable : IAsyncDisposable
    {
        // This method is here to make the code being used in the handler to not trigger compiler warnings
        public void Initialize(Context scenarioContext) => context = scenarioContext;

        public ValueTask DisposeAsync()
        {
            context.ScopedAsyncDisposableDisposed = true;
            context.MarkAsCompleted();
            return new ValueTask();
        }

        Context context;
    }
}