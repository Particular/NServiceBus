namespace NServiceBus.Encryption
{
    class Bootstrapper : INeedInitialization
    {
        public void Init(Configure config)
        {
            config.Configurer.ConfigureComponent<EncryptionMessageMutator>(DependencyLifecycle.InstancePerCall);
        }
    }
}
