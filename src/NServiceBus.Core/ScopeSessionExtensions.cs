namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;

    /// <summary>
    /// 
    /// </summary>
    public static class ScopeSessionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpointConfiguration"></param>
        public static void FloatScopedSession(this EndpointConfiguration endpointConfiguration)
        {
            endpointConfiguration.GetSettings().Set<ScopedSessionHolder>(new ScopedSessionHolder());
        }
    }
}