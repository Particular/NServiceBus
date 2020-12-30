using NServiceBus.Transports;

namespace NServiceBus.Transport
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the result for configuring the transport for sending.
    /// </summary>
    public class TransportSendInfrastructure
    {
        /// <summary>
        /// Creates new result object.
        /// <param name="dispatcherFactory">Factory to create the dispatcher.</param>
        /// <param name="preStartupCheck">Callback to perform checks before the transports starts sending messages.</param>
        /// </summary>
        public TransportSendInfrastructure(Func<IMessageDispatcher> dispatcherFactory,
            Func<Task<StartupCheckResult>> preStartupCheck)
        {
            Guard.AgainstNull(nameof(dispatcherFactory), dispatcherFactory);
            Guard.AgainstNull(nameof(preStartupCheck), preStartupCheck);
            DispatcherFactory = dispatcherFactory;
            PreStartupCheck = preStartupCheck;
        }

        /// <summary>
        /// Factory to create the dispatcher.
        /// </summary>
        public Func<IMessageDispatcher> DispatcherFactory { get; }

        /// <summary>
        /// Callback to perform checks before the transports starts sending messages.
        /// </summary>
        public Func<Task<StartupCheckResult>> PreStartupCheck { get; }
    }
}