namespace NServiceBus
{
    public static class InstallConfigExtensions
    {
        public static Configure EnableInstallers(this Configure config, string username = null)
        {
            if (username != null)
            {
                config.Settings.Set("installation.userName", username);
            }
            config.Features(x => x.Enable<InstallationSupport>());
            return config;
        }
    }
}