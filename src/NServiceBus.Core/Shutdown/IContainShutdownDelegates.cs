namespace NServiceBus
{
    using System.Threading.Tasks;

    interface IContainShutdownDelegates
    {
        Task Execute();
    }
}