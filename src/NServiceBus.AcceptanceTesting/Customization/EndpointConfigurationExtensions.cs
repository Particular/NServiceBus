namespace NServiceBus.AcceptanceTesting.Customization
{
    using System;
    using System.Collections.Generic;

    public static class EndpointConfigurationExtensions
    {
        /// <summary>
        /// Backdoor into the core types to scan. This allows people to test a subset of functionality when running Acceptance tests
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
        /// <param name="typesToScan">Override the types to scan.</param>
        public static void TypesToIncludeInScan(this EndpointConfiguration config, IEnumerable<Type> typesToScan)
        {
            config.TypesToScanInternal(typesToScan);
        }

        public static void AuditProcessedMessagesTo<TAuditEndpoint>(this EndpointConfiguration config)
        {
            var auditEndpointAddress = Conventions.EndpointNamingConvention(typeof(TAuditEndpoint));
            config.AuditProcessedMessagesTo(auditEndpointAddress);
        }

        public static void SendFailedMessagesTo<TErrorEndpoint>(this EndpointConfiguration config)
        {
            var errorEndpointAddress = Conventions.EndpointNamingConvention(typeof(TErrorEndpoint));
            config.SendFailedMessagesTo(errorEndpointAddress);
        }

        public static void RouteToEndpoint(this RoutingSettings routingSettings, Type messageType, Type destinationEndpointType)
        {
            var destinationEndpointAddress = Conventions.EndpointNamingConvention(destinationEndpointType);
            routingSettings.RouteToEndpoint(messageType, destinationEndpointAddress);
        }
    }
}