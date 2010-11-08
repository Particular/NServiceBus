using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Encryption
{
    class Bootstrapper : INeedInitialization
    {
        public void Init()
        {
            NServiceBus.Configure.Instance.Configurer.ConfigureComponent<EncryptionMessageMutator>(ComponentCallModelEnum.Singlecall);
        }
    }
}
