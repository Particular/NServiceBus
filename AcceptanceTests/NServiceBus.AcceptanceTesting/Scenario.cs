namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Support;

    public class Scenario
    {
        public static IScenarioWithEndpointBehavior<BehaviorContext> Define()
        {
            return Define<BehaviorContext>();
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>() where T : BehaviorContext
        {
            return new ScenarioWithContext<T>(Activator.CreateInstance<T>);
        }

        public static IScenarioWithEndpointBehavior<T> Define<T>(Func<T> contextFactory) where T : BehaviorContext
        {
            return new ScenarioWithContext<T>(contextFactory);
        }

    }

    public class ScenarioWithContext<TContext> : IScenarioWithEndpointBehavior<TContext>, IAdvancedScenarioWithEndpointBehavior<TContext> where TContext :BehaviorContext
    {
        public ScenarioWithContext(Func<TContext> factory)
        {
            contextFactory = factory;
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>() where T : EndpointBuilder
        {
            return WithEndpoint<T>(() => null);
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(BehaviorContext context) where T : EndpointBuilder
        {
            return WithEndpoint<T>(() => context);
        }

        public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Func<BehaviorContext> context) where T : EndpointBuilder
        {
            behaviours.Add(new BehaviorDescriptor(() => scenarioContext, typeof(T)));

            return this;
        }

        public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        {
            done = (c) => func((TContext)c);

            return this;
        }

        public void Run()
        {
            var builder = new RunDescriptorsBuilder();

            runDescriptorsBuilderAction(builder);

            var runDescriptors = builder.Descriptors;

            if (!runDescriptors.Any())
                runDescriptors.Add(new RunDescriptor
                {
                    Key = "Default"
                });

            scenarioContext = contextFactory();

            var sw = new Stopwatch();

            sw.Start();
            ScenarioRunner.Run(runDescriptors, this.behaviours, shoulds,this.done);

            sw.Stop();

            Console.Out.WriteLine("Total time for testrun: {0}",sw.Elapsed);
        }

        public IAdvancedScenarioWithEndpointBehavior<TContext> Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

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

        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => { };
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();
        public Func<BehaviorContext, bool> done = context => true;

        Func<TContext> contextFactory = () => Activator.CreateInstance<TContext>();

        TContext scenarioContext;

    }

    public class ScenarioVerification<T> : IScenarioVerification where T : BehaviorContext
    {
        public Action<T> Should { get; set; }
        public Type ContextType { get; set; }

        public void Verify(BehaviorContext context)
        {
           Should(((T)context));
        }
    }

    public interface IScenarioVerification
    {
        Type ContextType { get; set; }
        void Verify(BehaviorContext context);
    }
}