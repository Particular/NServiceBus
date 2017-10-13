namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// Extension methods to configure scoped session.
    /// </summary>
    public static class ScopeSessionExtensions
    {
        /// <summary>
        /// Enables floating of session.
        /// </summary>
        public static void FloatScopedSession(this EndpointConfiguration endpointConfiguration)
        {
            endpointConfiguration.GetSettings().Set<ScopedSessionHolder>(new ScopedSessionHolder());
        }
    }
}