namespace NServiceBus.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Support;

    public class Scenario : IScenarioWithEndpointBehavior, IAdvancedScenarioWithEndpointBehavior
    {
        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => { };
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();
        public Func<BehaviorContext, bool> done = context => false;

        public static IScenarioWithEndpointBehavior Define()
        {
            return new Scenario();
        }


        public IScenarioWithEndpointBehavior WithEndpoint<T>() where T : EndpointBuilder
        {
            return WithEndpoint<T>(() => null);
        }

        public IScenarioWithEndpointBehavior WithEndpoint<T>(BehaviorContext context) where T : EndpointBuilder
        {
            return WithEndpoint<T>(() => context);
        }

        public IScenarioWithEndpointBehavior WithEndpoint<T>(Func<BehaviorContext> context) where T : EndpointBuilder
        {
            behaviours.Add(new BehaviorDescriptor(context, typeof (T)));

            return this;
        }

        public IScenarioWithEndpointBehavior Done<T>(Func<T, bool> func) where T:BehaviorContext
        {
            done = (c)=>func((T)c);

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
            var sw = new Stopwatch();

            sw.Start();
            ScenarioRunner.Run(runDescriptors, this.behaviours, shoulds,this.done);

            sw.Stop();

            Console.Out.WriteLine("Total time for testrun: {0}",sw.Elapsed);
        }

        public IAdvancedScenarioWithEndpointBehavior Repeat(Action<RunDescriptorsBuilder> action)
        {
            runDescriptorsBuilderAction = action;

            return this;
        }

        public IAdvancedScenarioWithEndpointBehavior Should<T>(Action<T> should) where T : BehaviorContext
        {
           shoulds.Add(new ScenarioVerification<T>
                {
                    ContextType = typeof (T),
                    Should = should
                });

            return this;
        }
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