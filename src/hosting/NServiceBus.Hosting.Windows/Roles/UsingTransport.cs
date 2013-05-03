namespace NServiceBus
{
    using Hosting.Roles;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    /// Role used to specify the desired transport to use
    /// </summary>
    /// <typeparam name="T">The <see cref="TransportDefinition"/> to use.</typeparam>
    public interface UsingTransport<T> : IRole where T : TransportDefinition
    {
    }
}