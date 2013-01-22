namespace NServiceBus.IntegrationTests.Support
{
    using System;

    [Serializable]
    public class BehaviorDescriptor : MarshalByRefObject
    {
        private readonly Func<BehaviorContext> behaviorContextBuilder;

        public BehaviorDescriptor(Func<BehaviorContext> contextBuilder, Type builderType)
        {
            behaviorContextBuilder = contextBuilder;
            EndpointBuilderType = builderType;
            EndpointName = Conventions.EndpointNamingConvention(builderType);
        }
        public void Init()
        {
            Context = behaviorContextBuilder();
        }

        public BehaviorContext Context { get; private set; }

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