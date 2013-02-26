namespace NServiceBus
{
    using Hosting.Roles;
    using Transports;
    using Unicast.Transport;

    /// <summary>
    /// Role used to specify the desired transport to use
    /// </summary>
    /// <typeparam name="T">The <see cref="ITransportDefinition"/> to use.</typeparam>
    public interface UsingTransport<T> : IRole where T : ITransportDefinition
    {
    }
}