namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Customization;
    using Support;
    using Logging;

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext>, IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext, new()
    {
        public ScenarioWithContext(Action<TContext> initializer)
        {
            contextInitializer = initializer;
        }

        public Task<IEnumerable<TContext>> Run(TimeSpan? testExecutionTimeout = null)
        {
            var settings = new RunSettings();
            if (testExecutionTimeout.HasValue)
            {
                settings.TestExecutionTimeout = testExecutionTimeout.Value;
            }

            return Run(settings);
        }

        public async Task<IEnumerable<TContext>> Run(RunSettings settings)
        {
            var builder = new RunDescriptorsBuilder();

            runDescriptorsBuilderAction(builder);

            var runDescriptors = builder.Build();

            if (!runDescriptors.Any())
            {
                Console.WriteLine("No active rundescriptors were found for this test, test will not be executed");
                return new List<TContext>();
            }

            foreach (var runDescriptor in runDescriptors)
            {
                var scenarioContext = new TContext();
                contextInitializer(scenarioContext);
                runDescriptor.ScenarioContext = scenarioContext;
                runDescriptor.Settings.Merge(settings);
            }

            LogManager.UseFactory(new ContextAppenderFactory());

            var sw = new Stopwatch();

            sw.Start();
            await ScenarioRunner.Run(runDescriptors, behaviors, shoulds, done, limitTestParallelismTo, reports, allowedExceptions).ConfigureAwait(false);
            sw.Stop();

            Console.WriteLine("Total time for testrun: {0}", sw.Elapsed);

            return runDescriptors.Select(r => (TContext) r.ScenarioContext);
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism)
        {
            limitTestParallelismTo = maxParallelism;

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Should(Action<TContext> should)
        {
            shoulds.Add(new ScenarioVerification<TContext>
            {
                ContextType = typeof(TContext),
                Should = should
            });

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Report(Action<RunSummary> reportActions)
        {
            reports = reportActions;
            return this;
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

        public IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

            return this;
        }

        async Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(TimeSpan? testExecutionTimeout)
        {
            var settings = new RunSettings();
            if (testExecutionTimeout.HasValue)
            {
                settings.TestExecutionTimeout = testExecutionTimeout.Value;
            }

            var contexts = await Run(settings).ConfigureAwait(false);
            return contexts.Single();
        }

        async Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(RunSettings settings)
        {
            var contexts = await Run(settings).ConfigureAwait(false);
            return contexts.Single();
        }

        public IScenarioWithEndpointBehavior<TContext> AllowExceptions(Func<Exception, bool> filter = null)
        {
            if (filter == null)
            {
                filter = exception => true;
            }

            allowedExceptions = filter;
            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> AllowSimulatedExceptions()
        {
            return AllowExceptions(e => e is SimulatedException);
        }

        Func<Exception, bool> allowedExceptions = exception => false;
        List<EndpointBehavior> behaviors = new List<EndpointBehavior>();
        Action<TContext> contextInitializer;
        Func<ScenarioContext, bool> done = context => true;

        int limitTestParallelismTo;
        Action<RunSummary> reports;
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => builder.For(Conventions.DefaultRunDescriptor());
        List<IScenarioVerification> shoulds = new List<IScenarioVerification>();
    }
}