namespace NServiceBus
{
    using Settings;

    class EndpointInfo
    {
        public EndpointInfo(ReadOnlySettings settings)
        {
            Name = settings.Get<string>("NServiceBus.Routing.EndpointName");
            IsSendOnly = settings.GetOrDefault<bool>("Endpoint.SendOnly");
            InstanceDiscriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
        }

        public string Name { get; }
        public bool IsSendOnly { get; }
        public string InstanceDiscriminator { get; }
    }
}