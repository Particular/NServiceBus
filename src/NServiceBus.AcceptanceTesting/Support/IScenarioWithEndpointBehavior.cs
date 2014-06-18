namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public interface IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> behavior) where T : EndpointConfigurationBuilder;

        IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func);

        TContext Run(TimeSpan? testExecutionTimeout = null);

        IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> runtimeDescriptor);

        IScenarioWithEndpointBehavior<TContext> AllowExceptions(Func<Exception,bool> filter = null);
    }

    public interface IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext
    {
        IAdvancedScenarioWithEndpointBehavior<TContext> Should(Action<TContext> should);

        IAdvancedScenarioWithEndpointBehavior<TContext> Report(Action<RunSummary> summaries);


        IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism);

        IEnumerable<TContext> Run(TimeSpan? testExecutionTimeout = null);
    }
}