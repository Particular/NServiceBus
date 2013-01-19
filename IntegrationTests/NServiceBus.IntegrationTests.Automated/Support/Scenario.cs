namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;

    public class Scenario
    {
        private readonly ScenarioDescriptor descriptor;

        readonly IList<BehaviorDescriptor> behaviours = new List<BehaviorDescriptor>();

        private Scenario(ScenarioDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public static Scenario For<TDescriptor>()
            where TDescriptor : ScenarioDescriptor
        {
            return new Scenario(Activator.CreateInstance<TDescriptor>());
        }

        public  Scenario WithEndpointBehaviour<T>() where T:BehaviorFactory
        {
            this.behaviours.Add(new BehaviorDescriptor(new BehaviorContext(), Activator.CreateInstance<T>()));
            return this;
        }

        public Scenario WithEndpointBehaviour<T>(BehaviorContext context) where T : BehaviorFactory
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