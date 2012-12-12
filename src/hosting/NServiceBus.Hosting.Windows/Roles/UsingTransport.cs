namespace NServiceBus
{
    using Hosting.Roles;
    using Unicast.Transport;

    /// <summary>
    /// Role used to specify the desired transport to use
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface UsingTransport<T> : IRole where T : ITransportDefinition
    {
    }
}