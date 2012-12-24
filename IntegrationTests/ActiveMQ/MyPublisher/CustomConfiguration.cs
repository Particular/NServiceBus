namespace MyPublisher
{
    using NServiceBus;

    class CustomConfiguration : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<ConsoleMailer>(DependencyLifecycle.SingleInstance);
        }
    }
}