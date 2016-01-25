namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the result for configuring the transport for sending.
    /// </summary>
    public class TransportSendingConfigurationResult
    {
        /// <summary>
        /// Creates new result object.
        /// </summary>
        public TransportSendingConfigurationResult(
            Func<IDispatchMessages> dispatcherFactory,
            Func<Task<StartupCheckResult>> preStartupCheck,
            Func<SessionContext> sessionContextFactory)
        {
            Guard.AgainstNull(nameof(dispatcherFactory), dispatcherFactory);
            Guard.AgainstNull(nameof(preStartupCheck), preStartupCheck);
            Guard.AgainstNull(nameof(sessionContextFactory), sessionContextFactory);

            DispatcherFactory = dispatcherFactory;
            PreStartupCheck = preStartupCheck;
            SessionContextFactory = sessionContextFactory;
        }

        internal Func<IDispatchMessages> DispatcherFactory { get; }
        internal Func<Task<StartupCheckResult>> PreStartupCheck { get; }
        internal Func<SessionContext> SessionContextFactory { get; }
    }
}