namespace NServiceBus.AcceptanceTesting.Customization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Features;
using Hosting.Helpers;
using Installation;
using Sagas;
using Support;

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

    /// <summary>
    /// Uses <see cref="TypesToIncludeInScan"/> to scan all types via the <see cref="AssemblyScanner"/> that are currently loaded, filtering by customizations defined in <see cref="EndpointCustomizationConfiguration"/>.
    /// Additionally, this method excludes all types on the same assembly that not relevant to the specific test case. All types that should be scanned by default must be nested classes of the test class.
    /// </summary>
    public static void ScanTypesForTest(this EndpointConfiguration config,
        EndpointCustomizationConfiguration customizationConfiguration)
    {
        // disable file system scanning for better performance
        // note that this might cause issues when required assemblies are only being loaded at endpoint startup time
        var assemblyScanner = new AssemblyScanner
        {
            ScanFileSystemAssemblies = false
        };

        var testTypes = GetNestedTypeRecursive(customizationConfiguration.BuilderType.DeclaringType, customizationConfiguration.BuilderType).ToList();
        config.TypesToIncludeInScan(
        [
            .. assemblyScanner.GetScannableAssemblies().Types
                .Except(customizationConfiguration.BuilderType.Assembly.GetTypes()) // exclude all types from test assembly by default
                .Union(testTypes)
                .Where(t => t.IsAssignableTo(typeof(IFinder))
                            || t.IsAssignableTo(typeof(IHandleSagaNotFound))
                            || t.IsAssignableTo(typeof(Saga)))
                .Union(customizationConfiguration.TypesToInclude)
        ]);

        //auto-register handlers for now
        foreach (var messageHandler in testTypes.Where(t => t.IsAssignableTo(typeof(IHandleMessages))))
        {
            AddHandlerWithReflection(messageHandler, config);
        }

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

    static void AddHandlerWithReflection(Type handlerType, EndpointConfiguration endpointConfiguration) =>
        typeof(MessageHandlerRegistrationExtensions)
            .GetMethod("AddHandler", BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(handlerType)
            .Invoke(null, [endpointConfiguration]);
}