namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;
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

            LogManager.UseFactory(new ContextAppenderFactory());

            var sw = new Stopwatch();

            sw.Start();
            var runSummary = await ScenarioRunner.Run(runDescriptor, behaviors, done).ConfigureAwait(false);
            sw.Stop();

            await runDescriptor.RaiseOnTestCompleted(runSummary);

            DisplayRunResult(runSummary);
            Console.WriteLine("Total time for testrun: {0}", sw.Elapsed);

            if (runSummary.Result.Failed)
            {
                throw runSummary.Result.Exception;
            }

            return (TContext) runDescriptor.ScenarioContext;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
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

        static void DisplayRunResult(RunSummary summary)
        {
            var runDescriptor = summary.RunDescriptor;
            var runResult = summary.Result;

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Test summary:");
            Console.WriteLine();

            PrintSettings(runDescriptor.Settings);

            Console.WriteLine();
            Console.WriteLine("Endpoints:");

            foreach (var endpoint in runResult.ActiveEndpoints)
            {
                Console.WriteLine("     - {0}", endpoint);
            }

            if (runResult.Failed)
            {
                Console.WriteLine("Test failed: {0}", runResult.Exception);
            }
            else
            {
                Console.WriteLine("Result: Successful - Duration: {0}", runResult.TotalTime);
            }

            //dump trace and context regardless since asserts outside the should could still fail the test
            Console.WriteLine();
            Console.WriteLine("Context:");

            foreach (var prop in runResult.ScenarioContext.GetType().GetProperties())
            {
                if (prop.Name == "Trace")
                {
                    continue;
                }

                Console.WriteLine("{0} = {1}", prop.Name, prop.GetValue(runResult.ScenarioContext, null));
            }

            Console.WriteLine();
            Console.WriteLine("Trace:");
            Console.WriteLine(runResult.ScenarioContext.Trace);
            Console.WriteLine("------------------------------------------------------");
        }

        static void PrintSettings(IEnumerable<KeyValuePair<string, object>> settings)
        {
            Console.WriteLine();
            Console.WriteLine("Using settings:");
            foreach (var pair in settings)
            {
                Console.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        List<EndpointBehavior> behaviors = new List<EndpointBehavior>();
        Action<TContext> contextInitializer;
        Func<ScenarioContext, bool> done = context => true;
    }
}