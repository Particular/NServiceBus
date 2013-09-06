namespace NServiceBus.Transports.ActiveMQ
{
    using System.Threading;
    using System.Threading.Tasks;
    using Satellites;

    public class ActiveMqSchedulerManagement : ISatellite
    {
        public const string SubScope = "ActiveMqSchedulerManagement";
        public const string ClearScheduledMessagesSelectorHeader = "ClearScheduledMessagesSelector";

        private CancellationTokenSource cancellationTokenSource;
        private Task task;

        public ActiveMqSchedulerManagement()
        {
            Disabled = true;
        }

        public ActiveMqSchedulerManagementJobProcessor ActiveMqSchedulerManagementJobProcessor { get; set; }

        public Address InputAddress
        {
            get { return Address.Local.SubScope(SubScope); }
        }

        public bool Disabled { get; set; }

        public void Start()
        {
            ActiveMqSchedulerManagementJobProcessor.Start();
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            task = Task.Factory.StartNew(() => RunDeferredMessageCleanup(token),
                                         token, TaskCreationOptions.LongRunning,
                                         TaskScheduler.Current);
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            task.Wait(cancellationTokenSource.Token);

            ActiveMqSchedulerManagementJobProcessor.Stop();
        }

        public bool Handle(TransportMessage message)
        {
            ActiveMqSchedulerManagementJobProcessor.HandleTransportMessage(message);
            return true;
        }

        private void RunDeferredMessageCleanup(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ActiveMqSchedulerManagementJobProcessor.ProcessAllJobs(token);
                Thread.Sleep(100);
            }
        }
    }
}