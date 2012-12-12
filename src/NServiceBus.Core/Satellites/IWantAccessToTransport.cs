namespace NServiceBus.Satellites
{
    using Unicast.Transport;

    /// <summary>
    /// Add this interface to an <see cref="ISatellite"/> if you need access to the <see cref="ISatellite"/> <see cref="ITransport"/>.
    /// </summary>
    public interface IWantAccessToTransport
    {
        /// <summary>
        /// The <see cref="ITransport"/> for this <see cref="ISatellite"/>.
        /// </summary>
        ITransport Transport { get; set; }
    }
}