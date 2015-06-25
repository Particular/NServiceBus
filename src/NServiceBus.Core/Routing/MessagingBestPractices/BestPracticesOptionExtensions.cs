namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.Routing.MessagingBestPractices;

    /// <summary>
    /// Provides options for disabling the best practice enforcement
    /// </summary>
    public static class BestPracticesOptionExtensions
    {
        /// <summary>
        /// Turns off the best practice enforcement for the given message
        /// </summary>
        public static void DoNotEnforceBestPractices(this ExtendableOptions options)
        {
            options.Context.Set(new EnforceBestPracticesOptions{Enabled = false});
        }
    }
}