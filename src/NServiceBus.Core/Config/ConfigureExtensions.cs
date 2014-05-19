namespace NServiceBus
{
    /// <summary>
    ///     Configure Extensions.
    /// </summary>
    public static class ConfigureExtensions
    {
        /// <summary>
        ///     Configures this endpoint as a send only endpoint.
        /// </summary>
        /// <remarks>
        ///     Use this in endpoints whose only purpose is sending messages, websites are often a good example of send only endpoints.
        /// </remarks>
        public static IBus SendOnly(this Configure config)
        {
            config.Endpoint.AsSendOnly();

            config.Initialize();
            return config.Builder.Build<IBus>();
        }
    }
}