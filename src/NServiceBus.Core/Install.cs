namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Security.Principal;
    using Installation;

    /// <summary>
    /// Contains extension methods to the Configure class.
    /// </summary>
    public static class Install
    {
        /// <summary>
        /// Indicates which environment is going to be installed, specifying that resources 
        /// to be created will be provided permissions for the currently logged on user.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config) where T : IEnvironment
        {
            return ForInstallationOn<T>(config, null);
        }

        /// <summary>
        /// Indicates which environment is going to be installed, specifying that resources 
        /// to be created will be provided permissions for the user represented by the userToken
        /// (where not the currently logged on user) or the currently logged on user.
        /// </summary>
        /// <typeparam name="T">The environment type.</typeparam>
        /// <param name="config">Extension method object.</param>
        /// <param name="username">The username.</param>
        /// <returns>An Installer object whose Install method should be invoked.</returns>
        public static Installer<T> ForInstallationOn<T>(this Configure config, string username) where T : IEnvironment
        {
            if (config.Configurer == null)
                throw new InvalidOperationException("No container found. Please call '.DefaultBuilder()' after 'Configure.With()' before calling this method (or provide an alternative container).");

            IIdentity identity;
           
            // Passing a token results in a duplicate identity exception in some cases, you can't compare tokens so this could
            // still happen but at least the explicit WindowsIdentity.GetCurrent().Token is avoided now.
            if (String.IsNullOrEmpty(username))
            {
                identity = WindowsIdentity.GetCurrent();
            }
            else
            {
                identity = new GenericIdentity(username);
            }

            return new Installer<T>(identity);
        }
    }
    
    /// <summary>
    /// Resolves objects who implement INeedToInstall and invokes them for the given environment.
    /// Assumes that implementors have already been registered in the container.
    /// </summary>
    /// <typeparam name="T">The environment for which the installers should be invoked.</typeparam>
    public class Installer<T> where T : IEnvironment
    {
        static Installer()
        {
            RunOtherInstallers = true;
        }
        /// <summary>
        /// Initializes a new instance of the Installer
        /// </summary>
        /// <param name="identity">Identity of the user to be used to setup installer.</param>
        public Installer(IIdentity identity)
        {
            this.identity = identity;
        }

        private readonly IIdentity identity;

        /// <summary>
        /// Gets or sets RunOtherInstallers 
        /// </summary>
        public static bool RunOtherInstallers { private get; set; }

        private static bool installedOthersInstallers;

        /// <summary>
        /// Invokes installers for the given environment
        /// </summary>
        public void Install()
        {
            Configure.Instance.Initialize();

            if(RunOtherInstallers)            
                InstallOtherInstallers();
        }

        /// <summary>
        /// Invokes only 'Something' - other than infrastructure,  installers for the given environment.
        /// </summary>
        private void InstallOtherInstallers()
        {
            if (installedOthersInstallers)
                return;

            Configure.Instance.Builder.BuildAll<INeedToInstallSomething>().ToList()
                .ForEach(i=>i.Install(identity.Name));

            installedOthersInstallers = true;
        }
    }
}
