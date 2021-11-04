﻿namespace NServiceBus
{
    using Features;

    //TODO should this be an interface?
    /// <summary>
    /// Lists the transport addresses this endpoint is consuming messages from.
    /// </summary>
    public class ReceiveAddresses
    {
        /// <summary>
        /// Creates a new instance of <see cref="ReceiveAddresses"/>.
        /// </summary>
        public ReceiveAddresses(string mainReceiveAddress, string instanceReceiveAddress, string[] satelliteReceiveAddresses)
        {
            MainReceiveAddress = mainReceiveAddress;
            InstanceReceiveAddress = instanceReceiveAddress;
            SatelliteReceiveAddresses = satelliteReceiveAddresses;
        }

        /// <summary>
        /// The endpoint's input address.
        /// </summary>
        public string MainReceiveAddress { get; }

        /// <summary>
        /// The endpoint's additional, instance-specific input address. This will be <value>null</value> if the endpoint hasn't been configured to be uniquely addressable using <see cref="ReceiveSettingsExtensions.MakeInstanceUniquelyAddressable"/>.
        /// </summary>
        public string InstanceReceiveAddress { get; }

        /// <summary>
        /// The input addresses that have been configured via <see cref="FeatureConfigurationContext.AddSatelliteReceiver"/>.
        /// </summary>
        public string[] SatelliteReceiveAddresses { get; }
    }
}