namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_registering_externally_managed_singletons : NServiceBusAcceptanceTest
{
    static MyComponent myComponent = new();

    [Test]
    public async Task Should_work()
    {
        var result = await Scenario.Define<Context>()
        .WithEndpoint<ExternallyManagedSingletonEndpoint>(b =>
            b.Services(static services => services.AddSingleton(myComponent))
                .When((session, c) => session.SendLocal(new SomeMessage())))
        .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ServiceProvider, Is.Not.Null, "IServiceProvider should be injectable");
            Assert.That(result.CustomService, Is.SameAs(myComponent), "Should inject custom services");
        }
    }

    public class Context : ScenarioContext
    {
        public IServiceProvider ServiceProvider { get; set; }
        public MyComponent CustomService { get; set; }
    }

    public class ExternallyManagedSingletonEndpoint : EndpointConfigurationBuilder
    {
        public ExternallyManagedSingletonEndpoint() => EndpointSetup<DefaultServer>();

        [Handler]
        public class SomeMessageHandler : IHandleMessages<SomeMessage>
        {
            public SomeMessageHandler(Context context, MyComponent component, IServiceProvider serviceProvider)
            {
                testContext = context;
                myComponent = component;

                testContext.CustomService = component;
                testContext.ServiceProvider = serviceProvider;
            }

            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }

            readonly Context testContext;
        }
    }

    public class MyComponent;

    public class SomeMessage : IMessage;
}