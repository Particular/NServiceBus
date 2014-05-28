namespace NServiceBus
{
    using System;
    using Settings;

    public static class ScaleOutExtentions
    {

        /// <summary>
        ///     Allows the user to control how the current endpoint behaves when scaled out.
        /// </summary>
        public static Configure ScaleOut(this Configure config,Action<ScaleOutSettings> customScaleOutSettings)
        {
            customScaleOutSettings(new ScaleOutSettings(config));

            return config;
        }
    }
}