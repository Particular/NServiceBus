namespace NServiceBus.Encryption
{
    class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.Configurer.ConfigureComponent<EncryptionMessageMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}
