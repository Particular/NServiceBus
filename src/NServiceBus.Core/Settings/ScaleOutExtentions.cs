namespace NServiceBus
{
    using Settings;

    /// <summary>
    /// Provides a fluent api to allow the configuration of <see cref="ScaleOutSettings"/>.
    /// </summary>
    public static class ScaleOutExtentions
    {
        /// <summary>
        ///     Allows the user to control how the current endpoint behaves when scaled out.
        /// </summary>
        public static ScaleOutSettings ScaleOut(this BusConfiguration config)
        {
            Guard.AgainstNull(config, "config");
            return new ScaleOutSettings(config);
        }
    }
}