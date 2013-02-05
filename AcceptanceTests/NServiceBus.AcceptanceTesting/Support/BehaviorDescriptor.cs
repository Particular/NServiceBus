namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using Customization;

    [Serializable]
    public class BehaviorDescriptor : MarshalByRefObject
    {
        readonly Func<BehaviorContext> behaviorContextBuilder;

        public BehaviorDescriptor(Func<BehaviorContext> contextBuilder, Type builderType)
        {
            behaviorContextBuilder = contextBuilder;
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
        }
        public BehaviorContext CreateContext()
        {
            return behaviorContextBuilder();
        }

        public string EndpointName { get; private set; }

        public Type EndpointBuilderType { get; private set; }

        public object GetEndpointBehaviour()
        {
            if (behaviour == null)
            {
                behaviour = ((IEndpointBehaviorFactory)Activator.CreateInstance(EndpointBuilderType)).Get();

                behaviour.EndpointName = EndpointName;
            }
                
            return behaviour;
        }

        EndpointBehavior behaviour;

    }
}