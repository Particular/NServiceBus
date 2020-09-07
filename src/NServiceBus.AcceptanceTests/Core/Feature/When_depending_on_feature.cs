namespace NServiceBus.AcceptanceTests.Core.Feature
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;

    public class When_depending_on_feature : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_start_startup_tasks_in_order_of_dependency()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithFeatures>(b => b.CustomConfig(c =>
                {
                    c.EnableFeature<DependencyFeature>();
                    c.EnableFeature<TypedDependentFeature>();
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.That(context.StartCalled, Is.True);
            Assert.That(context.StopCalled, Is.True);
        }

        class Context : ScenarioContext
        {
            public bool StartCalled { get; set; }
            public bool StopCalled { get; set; }
            public bool InitializeCalled { get; set; }
        }

        public class EndpointWithFeatures : EndpointConfigurationBuilder
        {
            public EndpointWithFeatures()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class TypedDependentFeature : Feature
        {
            public TypedDependentFeature()
            {
                DependsOn<DependencyFeature>();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.AddSingleton<Runner>();
                context.RegisterStartupTask(b => b.GetService<Runner>());
            }

            class Runner : FeatureStartupTask
            {
                Dependency dependency;

                public Runner(Dependency dependency)
                {
                    this.dependency = dependency;
                }
                protected override Task OnStart(IMessageSession session)
                {
                    dependency.Start();
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IMessageSession session)
                {
                    dependency.Stop();
                    return Task.FromResult(0);
                }
            }
        }

        public class DependencyFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.AddSingleton<Dependency>();
                context.Container.AddSingleton<Runner>();
                context.RegisterStartupTask(b => b.GetService<Runner>());
            }

            class Runner : FeatureStartupTask
            {
                Dependency dependency;

                public Runner(Dependency dependency)
                {
                    this.dependency = dependency;
                }
                protected override Task OnStart(IMessageSession session)
                {
                    dependency.Initialize();
                    return Task.FromResult(0);
                }

                protected override Task OnStop(IMessageSession session)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class Dependency
        {
            Context context;

            public Dependency(Context context)
            {
                this.context = context;
            }

            public void Start()
            {
                if (!context.InitializeCalled)
                {
                    throw new InvalidOperationException("Not initialized");
                }
                context.StartCalled = true;
            }

            public void Stop()
            {
                if (!context.InitializeCalled)
                {
                    throw new InvalidOperationException("Not initialized");
                }

                context.StopCalled = true;
            }

            public void Initialize()
            {
                context.InitializeCalled = true;
            }
        }
    }
}