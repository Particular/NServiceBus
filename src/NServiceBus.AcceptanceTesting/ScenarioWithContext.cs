namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Support;
    using Logging;

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext, new()
    {
        public ScenarioWithContext(Action<TContext> initializer)
        {
            contextInitializer = initializer;
        }

        public Task<TContext> Run(TimeSpan? testExecutionTimeout = null)
        {
            var settings = new RunSettings();
            if (testExecutionTimeout.HasValue)
            {
                settings.TestExecutionTimeout = testExecutionTimeout.Value;
            }

            return Run(settings);
        }

        public async Task<TContext> Run(RunSettings settings)
        {
            var scenarioContext = new TContext();
            contextInitializer(scenarioContext);

            var runDescriptor = new RunDescriptor
            {
                ScenarioContext = scenarioContext
            };
            runDescriptor.Settings.Merge(settings);

            LogManager.UseFactory(new ContextAppenderFactory());

            var sw = new Stopwatch();

            sw.Start();
            await ScenarioRunner.Run(runDescriptor, behaviors, done).ConfigureAwait(false);
            sw.Stop();

            Console.WriteLine("Total time for testrun: {0}", sw.Elapsed);

            return (TContext)runDescriptor.ScenarioContext;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
        }

        public IScenarioWithEndpointBehavior<TContext> WithCustomComponent(
            string name,
            Func<TContext, CancellationToken, Task> onStart, 
            Func<TContext, Task> onStop, 
            Func<TContext, CancellationToken, Task> onStarted = null) 
        {
            var behavior = new CustomComponentBehavior<TContext>(name, onStart, onStop, onStarted);
            behaviors.Add(behavior);
            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehavior) where T : EndpointConfigurationBuilder
        {
            var builder = new EndpointBehaviorBuilder<TContext>(typeof(T));

            defineBehavior(builder);

            behaviors.Add(builder.Build());

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            done = c => func((TContext) c);

            return this;
        }

        Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(TimeSpan? testExecutionTimeout)
        {
            var settings = new RunSettings();
            if (testExecutionTimeout.HasValue)
            {
                settings.TestExecutionTimeout = testExecutionTimeout.Value;
            }

            return Run(settings);
        }

        Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(RunSettings settings)
        {
            return Run(settings);
        }

        List<IEndpointBehavior> behaviors = new List<IEndpointBehavior>();
        Action<TContext> contextInitializer;
        Func<ScenarioContext, bool> done = context => true;
    }
}