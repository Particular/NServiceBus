namespace NServiceBus.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Support;

    public class Scenario : IScenarioWithEndpointBehavior, IAdvancedScenarioWithEndpointBehavior
    {
        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();
        Action<RunDescriptorsBuilder> runDescriptorsBuilderAction = builder => { };
        IList<IScenarioVerification> shoulds = new List<IScenarioVerification>();

        public static IScenarioWithEndpointBehavior Define()
        {
            return new Scenario();
        }


        public IScenarioWithEndpointBehavior WithEndpointBehaviour<T>() where T : BehaviorFactory
        {
            this.behaviours.Add(new BehaviorDescriptor(() => new BehaviorContext(), Activator.CreateInstance<T>()));
            return this;
        }

        public IScenarioWithEndpointBehavior WithEndpointBehaviour<T>(Func<BehaviorContext> contextBuilder) where T : BehaviorFactory
        {
            this.behaviours.Add(new BehaviorDescriptor(contextBuilder, Activator.CreateInstance<T>()));
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
                    Name = "Default"
                });
            
            ScenarioRunner.Run(runDescriptors, behaviours, shoulds);
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