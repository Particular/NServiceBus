namespace NServiceBus.AcceptanceTests.Core.DependencyInjection;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using EndpointTemplates;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

public class When_defining_same_dependencies_in_endpoints : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_be_isolated_except_global_shared_once()
    {
        var result = await Scenario.Define<Context>()
            .WithComponent(new ComponentThatRegistersGloballySharedServices())
            .WithEndpoint<WithSameDependenciesEndpoint>(b =>
                b.Services(static services => services.AddSingleton<IDependency, MyDependency>())
                    .CustomConfig(c => c.OverrideLocalAddress("DeeplyNestedDependenciesEndpoint1"))
                    .When((session, c) => session.Send("DeeplyNestedDependenciesEndpoint1", new SomeMessage1())))
            .WithEndpoint<WithSameDependenciesEndpoint>(b =>
                b.Services(static services => services.AddSingleton<IDependency, MyDependency>())
                    .CustomConfig(c => c.OverrideLocalAddress("DeeplyNestedDependenciesEndpoint2"))
                    .When((session, c) => session.Send("DeeplyNestedDependenciesEndpoint2", new SomeMessage1())))
            .Done(c => c.Dependencies.Count == 2)
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Dependencies, Has.Count.EqualTo(2), "Dependencies should have been resolved");
            Assert.That(result.Dependencies.ElementAt(0), Is.Not.SameAs(result.Dependencies.ElementAt(1)));
            Assert.That(result.Dependencies.ElementAt(0).Dependency, Is.Not.SameAs(result.Dependencies.ElementAt(1).Dependency));
            Assert.That(result.Dependencies.ElementAt(0).Dependency.Dependency, Is.Not.SameAs(result.Dependencies.ElementAt(1).Dependency.Dependency));
        }

        Assert.That(result.Dependencies.ElementAt(0).Singleton, Is.SameAs(result.Dependencies.ElementAt(1).Singleton));
    }

    class Context : ScenarioContext
    {
        public ConcurrentBag<IDependency> Dependencies { get; } = [];
    }

    class ComponentThatRegistersGloballySharedServices : ComponentRunner, IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            run.Services.AddSingleton<ISingletonShared, SingletonShared>();
            return Task.FromResult<ComponentRunner>(this);
        }

        public override string Name => nameof(ComponentThatRegistersGloballySharedServices);
    }

    class WithSameDependenciesEndpoint : EndpointConfigurationBuilder
    {
        public WithSameDependenciesEndpoint() => EndpointSetup<DefaultServer>(b =>
        {
            b.EnableFeature<MyFeatureProvidingMoreDependencies>();

            // doing registrations here to exercise some of the possible registration APIs.
            b.RegisterComponents(static services => services.AddSingleton<IDependencyOfDependencyOfDependency, DependencyOfDependencyOfDependency>());
        });

        class SomeMessageHandler(IDependency dependency) : IHandleMessages<SomeMessage1>
        {
            public Task Handle(SomeMessage1 message1, IMessageHandlerContext context)
            {
                dependency.DoSomething();
                return Task.CompletedTask;
            }
        }

        class MyFeatureProvidingMoreDependencies : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) => context.Services.AddSingleton<IDependencyOfDependency, DependencyOfDependency>();
        }
    }

    interface ISingletonShared;
    class SingletonShared : ISingletonShared;

    interface IDependency
    {
        IDependencyOfDependency Dependency { get; }

        ISingletonShared Singleton { get; }

        void DoSomething();
    }

    class MyDependency(IDependencyOfDependency dependency, ISingletonShared singleton) : IDependency
    {
        public IDependencyOfDependency Dependency { get; } = dependency;

        public ISingletonShared Singleton { get; } = singleton;

        public void DoSomething() => Dependency.DoSomething();
    }

    interface IDependencyOfDependency
    {
        IDependencyOfDependencyOfDependency Dependency { get; }

        void DoSomething();
    }

    class DependencyOfDependency(IDependencyOfDependencyOfDependency dependency) : IDependencyOfDependency
    {
        public IDependencyOfDependencyOfDependency Dependency { get; } = dependency;

        public void DoSomething() => Dependency.DoSomething();
    }

    interface IDependencyOfDependencyOfDependency
    {
        void DoSomething();
    }

    // using provider for demonstration purposes to simulate various scenarios
    class DependencyOfDependencyOfDependency(IServiceProvider serviceProvider) : IDependencyOfDependencyOfDependency
    {
        public void DoSomething()
        {
            var context = serviceProvider.GetRequiredService<Context>();
            context.Dependencies.Add(serviceProvider.GetRequiredService<IDependency>());
        }
    }

    public class SomeMessage1 : ICommand;
}