namespace NServiceBus
{
    using System.Threading.Tasks;
    using Transport;

    interface IReceiveComponent
    {
        Task CreateQueuesIfNecessary(QueueBindings queueBindings, string username);
        Task Initialize();
        void Start();
        Task Stop();
        Task PerformPreStartupChecks();
    }
}