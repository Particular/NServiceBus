namespace NServiceBus.Transports
{
    using System;

    class LogicalToTransportAddressTranslation
    {
        readonly TransportDefinition transportDefinition;
        readonly Func<LogicalAddress, string, string> userSpecifiedTranslation;

        public LogicalToTransportAddressTranslation(TransportDefinition transportDefinition, Func<LogicalAddress, string, string> userSpecifiedTranslation)
        {
            this.transportDefinition = transportDefinition;
            this.userSpecifiedTranslation = userSpecifiedTranslation;
        }

        public string Translate(LogicalAddress logicalAddress)
        {
            var defaultTranslation = transportDefinition.ToTransportAddress(logicalAddress);
            return userSpecifiedTranslation(logicalAddress, defaultTranslation);
        }
    }
}