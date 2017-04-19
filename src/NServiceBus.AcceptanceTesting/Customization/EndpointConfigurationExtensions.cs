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

        public static void AuditProcessedMessagesTo<TAuditEndoint>(this EndpointConfiguration config)
        {
            var auditEndpointAddress = Conventions.EndpointNamingConvention(typeof(TAuditEndoint));
            config.AuditProcessedMessagesTo(auditEndpointAddress);
        }

        public static void RouteToEndpoint(this RoutingSettings routingSettings, Type messageType, Type destinationEndpointType)
        {
            var destinationEndpointAddress = Conventions.EndpointNamingConvention(destinationEndpointType);
            routingSettings.RouteToEndpoint(messageType, destinationEndpointAddress);
        }
    }
}