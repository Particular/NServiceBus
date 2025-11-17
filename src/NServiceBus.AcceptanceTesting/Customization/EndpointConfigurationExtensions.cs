namespace NServiceBus.AcceptanceTesting.Customization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hosting.Helpers;
using Sagas;
using Support;

public static class EndpointConfigurationExtensions
{
    /// <summary>
    /// Uses <see cref="EndpointConfiguration.T"/> to scan all types via the <see cref="AssemblyScanner"/> that are currently loaded, filtering by customizations defined in <see cref="EndpointCustomizationConfiguration"/>.
    /// Additionally, this method excludes all types on the same assembly that not relevant to the specific test case. All types that should be scanned by default must be nested classes of the test class.
    /// </summary>
    public static void ScanTypesForTest(this EndpointConfiguration config,
        EndpointCustomizationConfiguration customizationConfiguration)
    {
        var typesToIncludeInScanning = GetNestedTypeRecursive(customizationConfiguration.BuilderType.DeclaringType, customizationConfiguration.BuilderType)
            .Where(t => t.IsAssignableTo(typeof(IHandleMessages))
                        || t.IsAssignableTo(typeof(IFinder))
                        || t.IsAssignableTo(typeof(IHandleSagaNotFound))
                        || t.IsAssignableTo(typeof(Saga)))
            .Union(customizationConfiguration.TypesToInclude);

        config.TypesToScanInternal(typesToIncludeInScanning);

        IEnumerable<Type> GetNestedTypeRecursive(Type rootType, Type builderType)
        {
            if (rootType == null)
            {
                throw new InvalidOperationException("Make sure you nest the endpoint infrastructure inside the TestFixture as nested classes");
            }

            yield return rootType;

            if (typeof(IEndpointConfigurationFactory).IsAssignableFrom(rootType) && rootType != builderType)
            {
                yield break;
            }

            foreach (var nestedType in rootType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SelectMany(t => GetNestedTypeRecursive(t, builderType)))
            {
                yield return nestedType;
            }
        }
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

    public static void EnforcePublisherMetadataRegistration(this EndpointConfiguration config, string endpointName, PublisherMetadata publisherMetadata)
    {
        config.Pipeline.Register(new EnforcePublisherMetadataBehavior(endpointName, publisherMetadata),
            "Enforces all published events have corresponding mappings in the PublisherMetadata");
        config.Pipeline.Register(new EnforceSubscriptionPublisherMetadataBehavior(endpointName, publisherMetadata),
            "Enforces all subscribed events have corresponding mappings in the PublisherMetadata");
    }
}