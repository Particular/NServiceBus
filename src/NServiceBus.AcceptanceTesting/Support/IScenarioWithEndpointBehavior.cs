namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
{
    IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder, new();

    IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> behavior) where T : EndpointConfigurationBuilder, new();

    IScenarioWithEndpointBehavior<TContext> WithEndpoint(EndpointConfigurationBuilder endpointConfigurationBuilder, Action<EndpointBehaviorBuilder<TContext>> defineBehavior);

    IScenarioWithEndpointBehavior<TContext> WithComponent(IComponentBehavior componentBehavior);

    IScenarioWithEndpointBehavior<TContext> WithServices(Action<IServiceCollection> configureServices);

    IScenarioWithEndpointBehavior<TContext> WithServiceResolve(Func<IServiceProvider, CancellationToken, Task> resolve, ServiceResolveMode resolveMode = ServiceResolveMode.BeforeStart);

    IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func);

    Task<TContext> Run(TimeSpan? testExecutionTimeout = null);
    Task<TContext> Run(RunSettings settings);
}