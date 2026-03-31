namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class ServiceResolveComponent(Func<IServiceProvider, ScenarioContext, CancellationToken, Task> action, int instanceIndex, ServiceResolveMode serviceResolveMode) : ComponentRunner, IComponentBehavior
{
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        runDescriptor = run;
        return Task.FromResult<ComponentRunner>(this);
    }

    public override Task ComponentsStarted(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runDescriptor);
        ArgumentNullException.ThrowIfNull(runDescriptor.ServiceProvider);
        ArgumentNullException.ThrowIfNull(runDescriptor.ScenarioContext);

        return serviceResolveMode == ServiceResolveMode.BeforeStart ? action(runDescriptor.ServiceProvider, runDescriptor.ScenarioContext, cancellationToken) : Task.CompletedTask;
    }

    public override Task Start(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runDescriptor);
        ArgumentNullException.ThrowIfNull(runDescriptor.ServiceProvider);

        return serviceResolveMode == ServiceResolveMode.AtStart ? action(runDescriptor.ServiceProvider, runDescriptor.ScenarioContext, cancellationToken) : Task.CompletedTask;
    }

    public override Task Stop(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runDescriptor);
        ArgumentNullException.ThrowIfNull(runDescriptor.ServiceProvider);

        return serviceResolveMode == ServiceResolveMode.AtStop ? action(runDescriptor.ServiceProvider, runDescriptor.ScenarioContext, cancellationToken) : Task.CompletedTask;
    }

    public override string Name => $"Resolves{instanceIndex}";

    RunDescriptor? runDescriptor;
}