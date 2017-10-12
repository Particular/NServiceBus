namespace NServiceBus
{
    using Settings;

    class EndpointInfo
    {
        //note: should be internal when this EndpointInfois made public
        public static EndpointInfo FromSettings(ReadOnlySettings settings)
        {
            var name = settings.Get<string>("NServiceBus.Routing.EndpointName");
            var isSendOnly = settings.GetOrDefault<bool>("Endpoint.SendOnly");
            var instanceDiscriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var nserviceBusVersion = GitFlowVersion.MajorMinorPatch;

            return new EndpointInfo(name, isSendOnly, instanceDiscriminator, nserviceBusVersion);
        }

        public EndpointInfo(string name, bool isSendOnly, string instanceDiscriminator, string nserviceBusVersion)
        {
            Name = name;
            IsSendOnly = isSendOnly;
            InstanceDiscriminator = instanceDiscriminator;
            NServiceBusVersion = nserviceBusVersion;
        }

        public string Name { get; }
        public bool IsSendOnly { get; }
        public string InstanceDiscriminator { get; }
        public string NServiceBusVersion { get; }
    }
}