namespace NServiceBus.Impersonation.Windows
{
    class ConfigureWindowsIdentityEnricher : INeedInitialization
    {
        public void Customize(BusConfiguration configuration)
        {
            configuration.RegisterComponents(r=> r.ConfigureComponent<WindowsIdentityEnricher>(DependencyLifecycle.SingleInstance));
        }
    }
}