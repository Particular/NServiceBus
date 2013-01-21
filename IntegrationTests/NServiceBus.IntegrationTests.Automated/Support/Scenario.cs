namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Scenario : IScenarioWithEndpointBehavior
    {
        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();

        public static IScenarioWithEndpointBehavior Define()
        {
            return new Scenario();
        }

     
        public IScenarioWithEndpointBehavior WithEndpointBehaviour<T>() where T : BehaviorFactory
        {
            this.behaviours.Add(new BehaviorDescriptor(new BehaviorContext(), Activator.CreateInstance<T>()));
            return this;
        }

        public IScenarioWithEndpointBehavior WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory
        {
            this.behaviours.Add(new BehaviorDescriptor(context, Activator.CreateInstance<T>()));
            return this;
        }

        public void Run()
        {
           Run(new []
               {
                   new RunDescriptor
                       {
                           Name = "Default"
                       } 
               });
        }

        public void RunFor<T>() where T : ScenarioDescriptor
        {
            var sd = Activator.CreateInstance<T>() as ScenarioDescriptor;

            Run(sd.ToList());
        }

        public void Run(Action<RunDescriptorsBuilder> action)
        {
            var builder = new RunDescriptorsBuilder();

            action(builder);

            Run(builder.Descriptors);
        }

        void Run(IEnumerable<RunDescriptor> runDescriptors)
        {
            ScenarioRunner.Run(runDescriptors, this.behaviours);
        }
    }
}