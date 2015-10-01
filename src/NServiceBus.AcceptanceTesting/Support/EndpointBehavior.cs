namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;

    public class EndpointBehavior
    {
        public EndpointBehavior(Type builderType)
        {
            EndpointBuilderType = builderType;
            CustomConfig = new List<Action<BusConfiguration>>();
        }

        public Type EndpointBuilderType { get; private set; }

        public List<IWhenDefinition> Whens { get; set; }

        public List<Action<BusConfiguration>> CustomConfig { get; set; }
    }
}