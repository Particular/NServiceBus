namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> behavior) where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> WithCustomComponent(
            string name,
            Func<TContext, CancellationToken, Task> onStart,
            Func<TContext, Task> onStop,
            Func<TContext, CancellationToken, Task> onStarted = null);

        IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func);

        Task<TContext> Run(TimeSpan? testExecutionTimeout = null);
        Task<TContext> Run(RunSettings settings);
    }
}