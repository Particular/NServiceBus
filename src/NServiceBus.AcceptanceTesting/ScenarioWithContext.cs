namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
    using NUnit.Framework;
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

            ScenarioContext.Current = scenarioContext;

            LogManager.UseFactory(Scenario.GetLoggerFactory(scenarioContext));

            var sw = new Stopwatch();

            sw.Start();
            var runSummary = await ScenarioRunner.Run(runDescriptor, behaviors, done).ConfigureAwait(false);
            sw.Stop();

            await runDescriptor.RaiseOnTestCompleted(runSummary).ConfigureAwait(false);

            var statusLabel = runSummary.Result.Failed ? "Failed" : "Passed";
            TestContext.WriteLine("Test {0}: {1} in: {2:0.0}s", TestContext.CurrentContext.Test.FullName, statusLabel, sw.Elapsed.TotalSeconds);
            if (runSummary.Result.Failed || ScenarioRunner.VerboseLogging)
            {
                DisplayRunResult(runSummary);
            }

            if (runSummary.Result.Failed)
            {
                PrintLog(scenarioContext);
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

        void PrintLog(TContext scenarioContext)
        {
            TestContext.WriteLine($"Log entries (log level: {scenarioContext.LogLevel}):");
            TestContext.WriteLine("------------------------------------------------------");
            foreach (var logEntry in scenarioContext.Logs)
            {
                TestContext.WriteLine($"{logEntry.Timestamp:T} {logEntry.Level} {logEntry.Endpoint ?? "<unknown>"}: {logEntry.Message}");
            }
        }

        static void DisplayRunResult(RunSummary summary)
        {
            var runDescriptor = summary.RunDescriptor;
            var runResult = summary.Result;

            TestContext.WriteLine("------------------------------------------------------");
            TestContext.WriteLine("Test summary:");
            TestContext.WriteLine();

            PrintSettings(runDescriptor.Settings);

            TestContext.WriteLine();
            TestContext.WriteLine("Endpoints:");

            foreach (var endpoint in runResult.ActiveEndpoints)
            {
                TestContext.WriteLine("     - {0}", endpoint);
            }

            if (runResult.Failed)
            {
                TestContext.WriteLine("Test failed: {0}", runResult.Exception.SourceException);
            }
            else
            {
                TestContext.WriteLine("Result: Successful - Duration: {0}", runResult.TotalTime);
            }

            //dump trace and context regardless since asserts outside the should could still fail the test
            TestContext.WriteLine();
            TestContext.WriteLine("Context:");

            foreach (var prop in runResult.ScenarioContext.GetType().GetProperties())
            {
                TestContext.WriteLine("{0} = {1}", prop.Name, prop.GetValue(runResult.ScenarioContext, null));
            }
        }

        static void PrintSettings(IEnumerable<KeyValuePair<string, object>> settings)
        {
            TestContext.WriteLine();
            TestContext.WriteLine("Using settings:");
            foreach (var pair in settings)
            {
                TestContext.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            TestContext.WriteLine();
        }

        List<IComponentBehavior> behaviors = new List<IComponentBehavior>();
        Action<TContext> contextInitializer;
        Func<ScenarioContext, Task<bool>> done = context => TaskEx.TrueTask;
    }
}