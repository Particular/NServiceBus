namespace NServiceBus.Settings
{
    using System;

    /// <summary>
    ///     Configuration class for Endpoint settings.
    /// </summary>
    public class Endpoint
    {
        private readonly EndpointAdvancedSettings endpointAdvancedSettings = new EndpointAdvancedSettings();

        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public bool IsSendOnly { get; private set; }

        /// <summary>
        ///     Tells the endpoint to not enforce durability (using InMemory storages, non durable messages, ...).
        /// </summary>
        public void AsVolatile()
        {
            Configure.Transactions.Disable();
            Configure.Transactions.Advanced(s =>
                {
                    s.DoNotWrapHandlersExecutionInATransactionScope = true;
                    s.SuppressDistributedTransactions = true;
                });

            Advanced().DurableMessages = false;
        }

        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public void AsSendOnly()
        {
            IsSendOnly = true;
        }

        /// <summary>
        ///     <see cref="Endpoint" /> advance settings.
        /// </summary>
        /// <returns>The advance settings.</returns>
        public EndpointAdvancedSettings Advanced()
        {
            return endpointAdvancedSettings;
        }

        /// <summary>
        ///     <see cref="Endpoint" /> advance settings.
        /// </summary>
        /// <param name="action">A lambda to set the advance settings.</param>
        public void Advanced(Action<EndpointAdvancedSettings> action)
        {
            action(endpointAdvancedSettings);
        }

        /// <summary>
        ///     <see cref="Endpoint" /> advance settings.
        /// </summary>
        public class EndpointAdvancedSettings
        {
            /// <summary>
            ///     Default constructor.
            /// </summary>
            public EndpointAdvancedSettings()
            {
                DurableMessages = true;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether messages are guaranteed to be delivered in the event of a computer failure or network problem.
            /// </summary>
            public bool DurableMessages { get; set; }
        }
    }
}