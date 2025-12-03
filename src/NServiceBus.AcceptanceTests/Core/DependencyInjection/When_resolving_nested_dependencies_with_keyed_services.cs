namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_resolving_nested_dependencies_with_keyed_services : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work()
    {
        var result = await Scenario.Define<Context>()
            .WithEndpoint<DeeplyNestedDependenciesEndpoint>(b =>
                b.Services(static services =>
                    {
                        services.AddKeyedScoped<IDependency, MyDependency>("Dependency");
                        services.AddSingleton(new FeatureSpecificObject("FromAcceptanceTest")); // will be overriden
                    })
                    .When((session, c) => session.SendLocal(new SomeMessage())))
            .Done(c => c.MessageReceived)
            .Run();

        Assert.That(result.MessageReceived, Is.True, "Message should be received");
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    class DeeplyNestedDependenciesEndpoint : EndpointConfigurationBuilder
    {
        public DeeplyNestedDependenciesEndpoint() => EndpointSetup<DefaultServer>(b =>
        {
            b.EnableFeature<MyFeatureProvidingMoreDependencies>();

            // doing registrations here to exercise some of the possible registration APIs.
            b.RegisterComponents(static services => services.AddKeyedSingleton<IDependencyOfDependencyOfDependency, DependencyOfDependencyOfDependency>("Dependency"));
        });

        class SomeMessageHandler([FromKeyedServices("Dependency")] IDependency dependency) : IHandleMessages<SomeMessage>
        {
            public Task Handle(SomeMessage message, IMessageHandlerContext context)
            {
                dependency.DoSomething();
                return Task.CompletedTask;
            }
        }

        class MyFeatureProvidingMoreDependencies : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Services.AddKeyedSingleton("Dependency", new FeatureSpecificObject("FromFeature"));
                context.Services.AddKeyedScoped<IDependencyOfDependency, DependencyOfDependency>("Dependency");
            }
        }
    }

    interface IDependency
    {
        void DoSomething();
    }

    class MyDependency([FromKeyedServices] IDependencyOfDependency dependency) : IDependency
    {
        public void DoSomething() => dependency.DoSomething();
    }

    interface IDependencyOfDependency
    {
        void DoSomething();
    }

    class DependencyOfDependency([FromKeyedServices] IDependencyOfDependencyOfDependency dependency) : IDependencyOfDependency
    {
        public void DoSomething() => dependency.DoSomething();
    }

    interface IDependencyOfDependencyOfDependency
    {
        void DoSomething();
    }

    // using provider for demonstration purposes to simulate various scenarios
    class DependencyOfDependencyOfDependency(IServiceProvider serviceProvider, [FromKeyedServices] FeatureSpecificObject featureSpecificObject) : IDependencyOfDependencyOfDependency
    {
        public void DoSomething() => serviceProvider.GetRequiredService<Context>().MessageReceived = featureSpecificObject.SomeValue == "FromFeature";
    }

    record FeatureSpecificObject(string SomeValue);

    public class SomeMessage : IMessage;
}