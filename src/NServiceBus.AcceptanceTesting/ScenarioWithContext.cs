namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Customization;
    using NServiceBus.Logging;
    using Support;

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext>, IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext, new()
    {
        public ScenarioWithContext(Action<TContext> initializer)
        {
            contextInitializer = initializer;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehavior) where T : EndpointConfigurationBuilder
        {

            var builder = new EndpointBehaviorBuilder<TContext>(typeof (T));

            defineBehavior(builder);

            behaviors.Add(builder.Build());

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            done = c => func((TContext)c);

            return this;
        }

        public Task<IEnumerable<TContext>> Run(TimeSpan? testExecutionTimeout = null)
        {
            return Run(new RunSettings
            {
                TestExecutionTimeout = testExecutionTimeout
            });
        }

        public async Task<IEnumerable<TContext>> Run(RunSettings settings)
        {
            var builder = new RunDescriptorsBuilder();

            runDescriptorsBuilderAction(builder);

            var runDescriptors = builder.Build();

            if (!runDescriptors.Any())
            {
                Console.WriteLine("No active rundescriptors was found for this test, test will not be executed");
                return new List<TContext>();
            }

            var scenarioContext = new TContext();
            contextInitializer(scenarioContext);
            foreach (var runDescriptor in runDescriptors)
            {
                runDescriptor.ScenarioContext = scenarioContext;
                runDescriptor.TestExecutionTimeout = settings.TestExecutionTimeout ?? TimeSpan.FromSeconds(90);
            }

            LogManager.UseFactory(new ContextAppender());
            ContextAppender.SetContext(scenarioContext);

            var sw = new Stopwatch();

            sw.Start();
            await ScenarioRunner.Run(runDescriptors, behaviors, shoulds, done, limitTestParallelismTo, reports, allowedExceptions).ConfigureAwait(false);
            ContextAppender.SetContext(null);
            sw.Stop();

            Console.WriteLine("Total time for testrun: {0}", sw.Elapsed);

            return runDescriptors.Select(r => (TContext)r.ScenarioContext);
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

            return this;
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

        public IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism)
        {
            limitTestParallelismTo = maxParallelism;

            return this;
        }

        async Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(TimeSpan? testExecutionTimeout)
        {
            var contexts = await Run(new RunSettings
            {
                TestExecutionTimeout = testExecutionTimeout
            });
            return contexts.Single();
        }

        async Task<TContext> IScenarioWithEndpointBehavior<TContext>.Run(RunSettings settings)
        {
            var contexts = await Run(settings);
            return contexts.Single();
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
        
        int limitTestParallelismTo;
        IList<EndpointBehavior> behaviors = new List<EndpointBehavior>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => builder.For(Conventions.DefaultRunDescriptor());
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();
        Func<ScenarioContext, bool> done = context => true;
        Action<TContext> contextInitializer;
        Action<RunSummary> reports;
        Func<Exception, bool> allowedExceptions = exception => false;
    }
}