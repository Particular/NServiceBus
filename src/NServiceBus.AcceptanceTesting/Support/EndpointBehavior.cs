namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class EndpointBehavior
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<BusConfiguration, ScenarioContext>>();
        }

        public Type EndpointBuilderType { get; private set; }

        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<BusConfiguration, ScenarioContext>> CustomConfig { get; set; }

        public bool DoNotFailOnErrorMessages { get; set; }
    }
}