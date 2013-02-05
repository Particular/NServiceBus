namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Customization;

    [Serializable]
    public class BehaviorDescriptor : MarshalByRefObject
    {
        readonly Func<ScenarioContext> behaviorContextBuilder;

        public BehaviorDescriptor(Func<ScenarioContext> contextBuilder, Type builderType)
        {
            behaviorContextBuilder = contextBuilder;
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
        }
        public ScenarioContext CreateContext()
        {
            return behaviorContextBuilder();
        }

        public string EndpointName { get; private set; }

        public Type EndpointBuilderType { get; private set; }

    }
}