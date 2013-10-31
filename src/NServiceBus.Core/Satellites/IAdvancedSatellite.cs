namespace NServiceBus.Satellites
{
    using System;
    using Unicast.Transport;

    /// <summary>
    /// Interface for satellites that needs more control over how the receiver is being setup
    /// </summary>
    public interface IAdvancedSatellite : ISatellite
    {
        /// <summary>
        /// Gets the customizations to apply to the receiver
        /// </summary>
        Action<TransportReceiver> GetReceiverCustomization();
    }
}