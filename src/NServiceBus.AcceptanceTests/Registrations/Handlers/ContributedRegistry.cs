namespace ContributedRegistrations
{
    using NServiceBus;
    using NServiceBus.AcceptanceTests.Registrations.Handlers;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;

    [HandlerRegistryContributor]
    sealed class ContributedRegistry : IHandlerRegistryContributor<ContributedRegistry>
    {
        public static ContributedRegistry Instance { get; } = new();

        public void AddHandlers(EndpointConfiguration configuration)
        {
            var settings = configuration.GetSettings();
            settings.GetOrCreate<MessageHandlerRegistry>().AddMessageHandlerForMessage<When_registering_handlers_through_a_contributor.ContributedHandler, When_registering_handlers_through_a_contributor.ContributedMessage>();
            settings.GetOrCreate<MessageMetadataRegistry>().RegisterMessageTypeWithHierarchy(typeof(When_registering_handlers_through_a_contributor.ContributedMessage), []);
        }

        public void AddSagas(EndpointConfiguration configuration)
        {
        }
    }
}
