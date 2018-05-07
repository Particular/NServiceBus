namespace NServiceBus
{
    using System;
    using Routing;
    using Settings;
    using Transport;

    static class ReceiveConfigurationBuilder
    {
        public static ReceiveConfiguration Build(ReadOnlySettings settings, TransportInfrastructure transportInfrastructure)
        {
            var isSendOnlyEndpoint = settings.Get<bool>("Endpoint.SendOnly");

            if (isSendOnlyEndpoint)
            {
                if (settings.HasExplicitValue(ReceiveSettingsExtensions.CustomLocalAddressKey))
                {
                    throw new Exception($"Specifying a base name for the input queue using `{nameof(ReceiveSettingsExtensions.OverrideLocalAddress)}(baseInputQueueName)` is not supported for send-only endpoints.");
                }
                return null;
            }

            var endpointName = settings.EndpointName();
            var discriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var queueNameBase = settings.GetOrDefault<string>(ReceiveSettingsExtensions.CustomLocalAddressKey) ?? endpointName;
            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");

            //note: This is an old hack, we are passing the endpoint name to bind but we only care about the properties
            var mainInstanceProperties = transportInfrastructure.BindToLocalEndpoint(new EndpointInstance(endpointName)).Properties;

            var logicalAddress = LogicalAddress.CreateLocalAddress(queueNameBase, mainInstanceProperties);

            var localAddress = transportInfrastructure.ToTransportAddress(logicalAddress);

            string instanceSpecificQueue = null;
            if (discriminator != null)
            {
                instanceSpecificQueue = transportInfrastructure.ToTransportAddress(logicalAddress.CreateIndividualizedAddress(discriminator));
            }

            var transactionMode = GetRequiredTransactionMode(settings);

            var pushRuntimeSettings = GetDequeueLimitations(settings);

            return new ReceiveConfiguration(logicalAddress, queueNameBase, localAddress, instanceSpecificQueue, transactionMode, pushRuntimeSettings, purgeOnStartup);
        }

        static PushRuntimeSettings GetDequeueLimitations(ReadOnlySettings settings)
        {
            if (settings.TryGet(out MessageProcessingOptimizationExtensions.ConcurrencyLimit concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }

        static TransportTransactionMode GetRequiredTransactionMode(ReadOnlySettings settings)
        {
            var transportTransactionSupport = settings.Get<TransportInfrastructure>().TransactionMode;

            //if user haven't asked for a explicit level use what the transport supports
            if (!settings.TryGet(out TransportTransactionMode requestedTransportTransactionMode))
            {
                return transportTransactionSupport;
            }

            if (requestedTransportTransactionMode > transportTransactionSupport)
            {
                throw new Exception($"Requested transaction mode `{requestedTransportTransactionMode}` can't be satisfied since the transport only supports `{transportTransactionSupport}`");
            }

            return requestedTransportTransactionMode;
        }
    }
}