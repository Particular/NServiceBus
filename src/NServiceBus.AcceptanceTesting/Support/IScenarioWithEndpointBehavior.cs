namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> behavior) where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func);

        Task<TContext> Run(TimeSpan? testExecutionTimeout = null);
        Task<TContext> Run(RunSettings settings);

        IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> runtimeDescriptor);
    }

    public interface IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        IAdvancedScenarioWithEndpointBehavior<TContext> Should(Action<TContext> should);

        IAdvancedScenarioWithEndpointBehavior<TContext> Report(Action<RunSummary> summaries);

        IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism);

        Task<IEnumerable<TContext>> Run(TimeSpan? testExecutionTimeout = null);

        Task<IEnumerable<TContext>> Run(RunSettings settings);
    }
}