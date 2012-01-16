namespace NServiceBus.Persistence.Raven.Installation
{
    using System;
    using System.IO;

    public static class RavenDBInstallerConfig
    {
        const string InstallDirectory = "NServiceBus.Persistence";


        public static bool InstallRavenDB(this Configure config)
        {
            if (!config.RavenInstallEnabled())
                return false;

            if (!config.ShouldInstallRavenIfNeeded())
                return false;

            var installPath = RavenInstallPath(config);

            if (Directory.Exists(installPath))
                return false;
           
            //Check if the port is available, if so let the installer setup raven if its beeing run
            return RavenHelpers.EnsureCanListenToWhenInNonAdminContext(RavenPersistenceConstants.DefaultPort);
        }

      
    
        public static string RavenInstallPath(this Configure config)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                           InstallDirectory);
        }

    }
}