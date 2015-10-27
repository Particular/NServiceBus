namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides the subset of bus operations that is applicable for a send only bus.
    /// </summary>
    public interface ISendOnlyBus : IDisposable
    {
        /// <summary>
        /// Creates a <see cref="IBusContext"/> which can be used to access several bus operations like send, publish, subscribe and more.
        /// </summary>
        /// <returns>a new <see cref="IBusContext"/> to which all operations performed on it are scoped.</returns>
        IBusContext CreateSendContext();
    }
}
