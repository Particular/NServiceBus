using System.Security.Principal;
using NServiceBus.Installation;

namespace NServiceBus
{
    public interface IWantQueuesCreated
    {
        void Create(WindowsIdentity identity);
    }

    public interface IWantQueuesCreated<T> : IWantQueuesCreated where T : IEnvironment
    {
    }

    public class QueueInstallerInitialization : INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.ForAllTypes<IWantQueuesCreated>(t => Configure.Instance.Configurer.ConfigureComponent(t, DependencyLifecycle.SingleInstance));
        }
    }
}