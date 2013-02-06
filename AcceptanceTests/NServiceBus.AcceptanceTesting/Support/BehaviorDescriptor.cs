namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Customization;

    [Serializable]
    public class BehaviorDescriptor : MarshalByRefObject
    {
        readonly Func<ScenarioContext> scenarioContextBuilder;

        public BehaviorDescriptor(Func<ScenarioContext> contextBuilder, Type builderType)
        {
            scenarioContextBuilder = contextBuilder;
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
        }
        public ScenarioContext CreateContext()
        {
            return scenarioContextBuilder();
        }

        public string EndpointName { get; private set; }

        public Type EndpointBuilderType { get; private set; }

    }
}