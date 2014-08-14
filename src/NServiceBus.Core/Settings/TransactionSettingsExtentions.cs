namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Provides a hook to allows users fine grained control over transactionality
    /// </summary>
    public static class TransactionSettingsExtentions
    {
        /// <summary>
        /// Entry point for transaction related configuration
        /// </summary>
        /// <param name="config"><see cref="Configure"/> instance.</param>
        /// <param name="enabled"><code>true</code> to enable transaction, otherwise <code>false</code>.</param>
        public static TransactionSettings Transactions(this ConfigurationBuilder config, bool enabled = true)
        {
            config.Settings.Set("Transactions.Enabled", enabled);
            config.Settings.SetDefault("Transactions.DoNotWrapHandlersExecutionInATransactionScope", !enabled);
            return new TransactionSettings(config);
        }
    }
}