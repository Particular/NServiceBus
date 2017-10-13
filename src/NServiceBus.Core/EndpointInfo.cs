namespace NServiceBus
{
    using Settings;

    class EndpointInfo
    {
        public EndpointInfo(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public string Name => settings.Get<string>("NServiceBus.Routing.EndpointName");

        public bool IsSendOnly => settings.GetOrDefault<bool>("Endpoint.SendOnly");

        public string InstanceDiscriminator => settings.GetOrDefault<string>("EndpointInstanceDiscriminator");

        ReadOnlySettings settings;
    }
}