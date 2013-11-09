namespace NServiceBus.Impersonation.Windows
{
    class ConfigureWindowsIdentityEnricher : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            Configure.Instance.Configurer.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance);
        }

    }
}