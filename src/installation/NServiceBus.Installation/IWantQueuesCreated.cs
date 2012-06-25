using System.Security.Principal;

namespace NServiceBus.Installation
{
    public interface IWantQueuesCreated
    {
        void Create(WindowsIdentity identity);
    }

    public interface IWantQueuesCreated<T> : IWantQueuesCreated where T : IEnvironment
    {
    }
}