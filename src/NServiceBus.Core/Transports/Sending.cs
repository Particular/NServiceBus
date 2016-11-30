namespace NServiceBus
{
    using ObjectBuilder;
    using Settings;
    using Transport;

    /// <summary>
    /// bla.
    /// </summary>
    public class TransportComponent
    {
        /// <summary>
        /// Bla.
        /// </summary>
        public IDispatchMessages Dispatcher { get; set; }

        /// <summary>
        /// blabla.
        /// </summary>
        public void Setup(ReadOnlySettings settings, IConfigureComponents confgire)
        {
            var transport = settings.Get<OutboundTransport>();
            var sendInfrastructure = transport.Configure(settings);
            sendInfrastructure.PreStartupCheck();
            Dispatcher = sendInfrastructure.DispatcherFactory();

            confgire.ConfigureComponent(() => Dispatcher, DependencyLifecycle.SingleInstance);
        }
    }
}