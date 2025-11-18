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
    /// Backdoor into the core types to scan. This allows people to test a subset of functionality when running Acceptance tests
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration"/> instance to apply the settings to.</param>
    /// <param name="typesToScan">Override the types to scan.</param>
    public static void TypesToIncludeInScan(this EndpointConfiguration config, IEnumerable<Type> typesToScan) => config.TypesToScanInternal(typesToScan);

    /// <summary>
    /// Finds all nested types related to a given acceptance test that hasn't yet been converted to being added via an explicit API.
    /// </summary>
    public static void ScanTypesForTest(this EndpointConfiguration config,
        EndpointCustomizationConfiguration customizationConfiguration)
    {
        var testTypes = GetNestedTypeRecursive(customizationConfiguration.BuilderType.DeclaringType, customizationConfiguration.BuilderType).ToList();

        var typesToIncludeInScanning = testTypes
            .Where(t => t.IsAssignableTo(typeof(IHandleSagaNotFound))
                        || t.IsAssignableTo(typeof(Saga)))
            .Union(customizationConfiguration.TypesToInclude);

        config.TypesToIncludeInScan(typesToIncludeInScanning);

        //auto-register handlers for now
        if (customizationConfiguration.AutoRegisterHandlers)
        {
            foreach (var messageHandler in testTypes.Where(t => t.IsAssignableTo(typeof(IHandleMessages))))
            {
                AddHandlerWithReflection(messageHandler, config);
            }
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