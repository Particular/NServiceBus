namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Role used to specify the desired transport to use
    /// </summary>
    /// <typeparam name="T">The <see cref="TransportDefinition"/> to use.</typeparam>
    public interface UsingTransport<T> where T : TransportDefinition
    {
    }
}