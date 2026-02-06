namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class ServiceRegistrationComponent(Action<IServiceCollection> configureServices, int instanceIndex) : ComponentRunner, IComponentBehavior
{
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        configureServices(run.Services);
        return Task.FromResult<ComponentRunner>(this);
    }

    public override string Name => $"Services{instanceIndex}";
}