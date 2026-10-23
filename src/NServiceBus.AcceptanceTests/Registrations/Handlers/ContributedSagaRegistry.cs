namespace ContributedSagaRegistrations
{
    using NServiceBus;
    using NServiceBus.AcceptanceTests.Registrations.Handlers;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Sagas;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using Saga = NServiceBus.AcceptanceTests.Registrations.Handlers.When_registering_sagas_through_a_contributor.EndpointUsingContributor.ContributedSaga;
    using SagaData = NServiceBus.AcceptanceTests.Registrations.Handlers.When_registering_sagas_through_a_contributor.EndpointUsingContributor.ContributedSagaData;
    using Start = NServiceBus.AcceptanceTests.Registrations.Handlers.When_registering_sagas_through_a_contributor.StartContributedSaga;
    using Complete = NServiceBus.AcceptanceTests.Registrations.Handlers.When_registering_sagas_through_a_contributor.CompleteContributedSaga;

    [HandlerRegistryContributor]
    sealed class ContributedSagaRegistry : IHandlerRegistryContributor<ContributedSagaRegistry>
    {
        public static ContributedSagaRegistry Instance { get; } = new();

        public void AddHandlers(EndpointConfiguration configuration)
        {
        }

        public void AddSagas(EndpointConfiguration configuration)
        {
            var settings = configuration.GetSettings();
            var sagaMetadataCollection = settings.GetOrCreate<SagaMetadataCollection>();
            var associatedMessages = new SagaMessage[]
            {
                new(typeof(Complete), false, false),
                new(typeof(Start), true, false),
            };
            MessagePropertyAccessor[] propertyAccessors = [CompleteOrderIdAccessor.Instance, StartOrderIdAccessor.Instance];
            var metadata = SagaMetadata.Create<Saga, SagaData>(associatedMessages, OrderIdAccessor.Instance, propertyAccessors);
            sagaMetadataCollection.Add(metadata);

            var messageHandlerRegistry = settings.GetOrCreate<MessageHandlerRegistry>();
            var messageMetadataRegistry = settings.GetOrCreate<MessageMetadataRegistry>();
            messageHandlerRegistry.AddMessageHandlerForMessage<Saga, Complete>();
            messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof(Complete), []);
            messageHandlerRegistry.AddMessageHandlerForMessage<Saga, Start>();
            messageMetadataRegistry.RegisterMessageTypeWithHierarchy(typeof(Start), []);
        }

        sealed class CompleteOrderIdAccessor : MessagePropertyAccessor<Complete>
        {
            public static readonly MessagePropertyAccessor Instance = new CompleteOrderIdAccessor();

            protected override object AccessFrom(Complete message) => message.OrderId;
        }

        sealed class StartOrderIdAccessor : MessagePropertyAccessor<Start>
        {
            public static readonly MessagePropertyAccessor Instance = new StartOrderIdAccessor();

            protected override object AccessFrom(Start message) => message.OrderId;
        }

        sealed class OrderIdAccessor : CorrelationPropertyAccessor
        {
            public static readonly CorrelationPropertyAccessor Instance = new OrderIdAccessor();

            public override object AccessFrom(IContainSagaData sagaData) => ((SagaData)sagaData).OrderId;

            public override void WriteTo(IContainSagaData sagaData, object value) => ((SagaData)sagaData).OrderId = (string)value;
        }
    }
}
