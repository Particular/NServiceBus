namespace NServiceBus.AcceptanceTests.Core.SelfVerification;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Support;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class When_using_custom_components : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_properly_start_and_stop_them()
    {
        var ctx = await Scenario.Define<Context>()
            .WithComponent(new CustomComponentBehavior())
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.Starting, Is.True);
            Assert.That(ctx.ComponentsStarted, Is.True);
            Assert.That(ctx.Stopped, Is.True);
            Assert.That(ctx.CustomDependenciesCanBeResolved, Is.True);
        }
    }

    class Context : ScenarioContext
    {
        public bool Starting { get; set; }
        public bool ComponentsStarted { get; set; }
        public bool Stopped { get; set; }
        public bool CustomDependenciesCanBeResolved { get; set; }
    }

    class CustomComponentBehavior : IComponentBehavior
    {
        public Task<ComponentRunner> CreateRunner(RunDescriptor run)
        {
            run.Services.AddSingleton<CustomDependency>();
            return Task.FromResult<ComponentRunner>(new Runner(run));
        }

        class Runner(RunDescriptor runDescriptor) : ComponentRunner
        {
            public override string Name => "MyComponent";

            public override Task Start(CancellationToken cancellationToken = default)
            {
                var context = runDescriptor.ServiceProvider!.GetRequiredService<Context>();
                context.Starting = true;
                context.CustomDependenciesCanBeResolved = runDescriptor.ServiceProvider.GetService<CustomDependency>() != null;
                return base.Start(cancellationToken);
            }

            public override Task ComponentsStarted(CancellationToken cancellationToken = default)
            {
                var context = runDescriptor.ServiceProvider!.GetRequiredService<Context>();
                context.ComponentsStarted = true;
                context.MarkAsCompleted();
                return base.ComponentsStarted(cancellationToken);
            }

            public override Task Stop(CancellationToken cancellationToken = default)
            {
                var context = runDescriptor.ServiceProvider!.GetRequiredService<Context>();
                context.Stopped = true;
                return base.Stop(cancellationToken);
            }
        }

        class CustomDependency;
    }
}