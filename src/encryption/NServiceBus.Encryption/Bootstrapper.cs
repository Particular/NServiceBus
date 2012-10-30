namespace NServiceBus.Encryption
{
    using NServiceBus.Config;

    class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<EncryptionMessageMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}
