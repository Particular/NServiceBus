namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using Support;

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext, new()
    {
        public ScenarioWithContext(Action<TContext> initializer)
        {
            contextInitializer = initializer;
        }

        public Task<TContext> Run(TimeSpan? testExecutionTimeout)
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

            var runDescriptor = new RunDescriptor(scenarioContext);
            runDescriptor.Settings.Merge(settings);

            TestExecutionContext.CurrentContext.AddRunDescriptor(runDescriptor);
            ScenarioContext.Current = scenarioContext;

            LogManager.UseFactory(Scenario.GetLoggerFactory(scenarioContext));

            var sw = new Stopwatch();
            var scenarioRunner = new ScenarioRunner(runDescriptor, behaviors, done);

            sw.Start();
            var runSummary = await scenarioRunner.Run().ConfigureAwait(false);
            sw.Stop();

            await runDescriptor.RaiseOnTestCompleted(runSummary).ConfigureAwait(false);

            TestContext.WriteLine("Test {0}: Scenario completed in {1:0.0}s", TestContext.CurrentContext.Test.FullName, sw.Elapsed.TotalSeconds);

            if (runSummary.Result.Failed)
            {
                runSummary.Result.Exception.Throw();
            }

            return (TContext)runDescriptor.ScenarioContext;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehavior) where T : EndpointConfigurationBuilder
        {
            return WithEndpoint(Activator.CreateInstance<T>(), defineBehavior);
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint(EndpointConfigurationBuilder endpointConfigurationBuilder, Action<EndpointBehaviorBuilder<TContext>> defineBehavior)
        {
            var builder = new EndpointBehaviorBuilder<TContext>(endpointConfigurationBuilder);
            defineBehavior(builder);
            behaviors.Add(builder.Build());

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> WithComponent(IComponentBehavior componentBehavior)
        {
            behaviors.Add(componentBehavior);
            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            return Done(ctx => Task.FromResult(func(ctx)));
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, Task<bool>> func)
        {
            done = c => func((TContext)c);

            return this;
        }

        List<IComponentBehavior> behaviors = new List<IComponentBehavior>();
        Action<TContext> contextInitializer;
        Func<ScenarioContext, Task<bool>> done = static context => Task.FromResult(true);
    }
}