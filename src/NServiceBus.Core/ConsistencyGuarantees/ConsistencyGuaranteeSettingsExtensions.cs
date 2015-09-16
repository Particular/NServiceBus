namespace NServiceBus.ConsistencyGuarantees
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    static class ConsistencyGuaranteeSettingsExtensions
    {
        public static ConsistencyGuarantee GetConsistencyGuarantee(this ReadOnlySettings settings)
        {
            ConsistencyGuarantee explicitConsistencyGuarantee;

            if (settings.TryGet(out explicitConsistencyGuarantee))
            {
                return explicitConsistencyGuarantee;
            }

            return settings.Get<TransportDefinition>().GetDefaultConsistencyGuarantee();
        }
    }
}