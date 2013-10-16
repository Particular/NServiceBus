namespace NServiceBus.Impersonation.Windows
{
    internal class ConfigureWindowsIdentityEnricher : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            Configure.Instance.Configurer.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance);
        }

    }
}