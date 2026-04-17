namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

sealed class ServiceRegistrationComponent(Action<IServiceCollection, ScenarioContext> configureServices, int instanceIndex) : ComponentRunner, IComponentBehavior
{
    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        configureServices(run.Services, run.ScenarioContext);
        return Task.FromResult<ComponentRunner>(this);
    }

    public override string Name => $"Services{instanceIndex}";
}