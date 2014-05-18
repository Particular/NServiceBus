namespace NServiceBus.Impersonation.Windows
{
    class ConfigureWindowsIdentityEnricher : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            config.Configurer.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance);
        }
    }
}