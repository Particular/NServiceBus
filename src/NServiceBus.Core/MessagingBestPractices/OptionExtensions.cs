namespace NServiceBus
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Provides options for disabling the best practice enforcement
    /// </summary>
    public static class OptionExtensions
    {
        /// <summary>
        /// Turns off the best practice enforcement for the given message
        /// </summary>
        /// <param name="options"></param>
        public static void DoNotEnforceBestPractices(this ExtendableOptions options)
        {
            options.Extensions.Set(new EnforceBestPracticesBehavior.Options{Enabled = false});
        }
    }
}