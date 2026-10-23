#nullable enable

namespace NServiceBus;

using System;
using Transport;

/// <summary>
/// Configuration extensions for header dictionary pooling.
/// </summary>
public static class HeaderPoolingConfigExtensions
{
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    extension(EndpointConfiguration config)
    {
        /// <summary>
        /// Enables pooling of outgoing message header dictionaries. Dictionaries are rented
        /// when messages are sent and returned to the pool once the message has been dispatched
        /// and the pipeline invocation that sent it has completed, that is, when the incoming message
        /// has been processed or the session operation has returned. Keeping a reference to a header
        /// dictionary beyond that point is outside the contract.
        /// </summary>
        public void EnableHeaderPooling()
        {
            ArgumentNullException.ThrowIfNull(config);

            config.Settings.Set<HeaderPool>(HeaderPool.Shared);
        }

        /// <summary>
        /// Enables pooling of outgoing message header dictionaries using the given pool instead of
        /// <see cref="HeaderPool.Shared" />, for example to raise the number of retained dictionaries
        /// on an endpoint with high processing concurrency, or to keep endpoints hosted in the same
        /// process from sharing a pool. The same lifetime contract as the parameterless overload applies.
        /// </summary>
        /// <param name="pool">The pool to rent header dictionaries from.</param>
        public void EnableHeaderPooling(HeaderPool pool)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(pool);

            config.Settings.Set(pool);
        }

        /// <summary>
        /// Disables pooling of outgoing message header dictionaries. When disabled, header
        /// dictionaries are allocated and garbage collected as before.
        /// </summary>
        public void DisableHeaderPooling()
        {
            ArgumentNullException.ThrowIfNull(config);

            config.Settings.Set<HeaderPool>(HeaderPool.AlwaysAllocate);
        }
    }
}