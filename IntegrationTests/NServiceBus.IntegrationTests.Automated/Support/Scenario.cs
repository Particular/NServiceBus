namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;

    public class Scenario : IScenarioWithEndpointBehavior
    {
        private readonly ScenarioDescriptor descriptor;

        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();

        private Scenario(ScenarioDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public static IScenarioWithEndpointBehavior Define()
        {
            return For<DefaultScenarioDescriptor>();
        }

        public static IScenarioWithEndpointBehavior For<TDescriptor>()
            where TDescriptor : ScenarioDescriptor
        {
            return new Scenario(Activator.CreateInstance<TDescriptor>());
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
            ScenarioRunner.Run(this.descriptor, this.behaviours);
        }
    }
}