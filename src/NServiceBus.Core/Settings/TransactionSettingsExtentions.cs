namespace NServiceBus
{
    using System;
    using Settings;

    /// <summary>
    /// Provides a hook to allows users fine grained control over transactionality
    /// </summary>
    public static class TransactionSettingsExtentions
    {
        /// <summary>
        /// Entry point for transaction related configuration
        /// </summary>
        /// <param name="config"></param>
        /// <param name="customizations">The user supplied config actions</param>
        /// <returns></returns>
        public static Configure Transactions(this Configure config,Action<TransactionSettings> customizations)
        {
            customizations(new TransactionSettings(config));
            return config;
        }
    }
}