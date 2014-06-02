namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Customization;
    using Support;

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext>, IAdvancedScenarioWithEndpointBehavior<TContext> where TContext : ScenarioContext, new()
    {
        public ScenarioWithContext(Func<TContext> factory)
        {
            contextFactory = factory;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointConfigurationBuilder
        {
            return WithEndpoint<T>(b => { });
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehavior) where T : EndpointConfigurationBuilder
        {

            var builder = new EndpointBehaviorBuilder<TContext>(typeof (T));

            defineBehavior(builder);

            behaviours.Add(builder.Build());

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            done = c => func((TContext)c);

            return this;
        }

        public IEnumerable<TContext> Run(TimeSpan? testExecutionTimeout = null)
        {
            var builder = new RunDescriptorsBuilder();

            runDescriptorsBuilderAction(builder);

            var runDescriptors = builder.Build();

            if (!runDescriptors.Any())
            {
                Console.Out.WriteLine("No active rundescriptors was found for this test, test will not be executed");
                return new List<TContext>();
            }

            foreach (var runDescriptor in runDescriptors)
            {
                runDescriptor.ScenarioContext = contextFactory();
                runDescriptor.TestExecutionTimeout = testExecutionTimeout ?? TimeSpan.FromSeconds(90);
            }

            var sw = new Stopwatch();

            sw.Start();
            ScenarioRunner.Run(runDescriptors, behaviours, shoulds, done, limitTestParallelismTo, reports);

            sw.Stop();

            Console.Out.WriteLine("Total time for testrun: {0}", sw.Elapsed);

            return runDescriptors.Select(r => (TContext)r.ScenarioContext);
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> MaxTestParallelism(int maxParallelism)
        {
            limitTestParallelismTo = maxParallelism;

            return this;
        }


        TContext IScenarioWithEndpointBehavior<TContext>.Run(TimeSpan? testExecutionTimeout)
        {
            return Run(testExecutionTimeout).Single();
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
        readonly IList<EndpointBehavior> behaviours = new List<EndpointBehavior>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => builder.For(Conventions.DefaultRunDescriptor());
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();
        public Func<ScenarioContext, bool> done = context => true;

        Func<TContext> contextFactory = () => new TContext();
        Action<RunSummary> reports;
    }
}