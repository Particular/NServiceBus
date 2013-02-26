namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using NServiceBus.Satellites;

    public class ActiveMqSchedulerManagement : ISatellite
    {
        public const string SubScope = "ActiveMqSchedulerManagement";
        public const string ClearScheduledMessagesSelectorHeader = "ClearScheduledMessagesSelector";

        private CancellationTokenSource cancellationTokenSource;
        private Task task;

        public ActiveMqSchedulerManagementJobProcessor ActiveMqSchedulerManagementJobProcessor { get; set; }

        public ActiveMqSchedulerManagement()
        {
            this.Disabled = true;
        }

        public Address InputAddress
        {
            get
            {
                return Address.Local.SubScope(SubScope);
            }
        }

        public bool Disabled { get; set; }

        public void Start()
        {
            this.ActiveMqSchedulerManagementJobProcessor.Start();
            this.cancellationTokenSource = new CancellationTokenSource();
            this.task = Task.Factory.StartNew(() => this.RunDeferredMessageCleanup(cancellationTokenSource.Token), this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public void Stop()
        {
            try
            {
                this.cancellationTokenSource.Cancel();
                this.task.Wait(this.cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) 
            { }

            this.ActiveMqSchedulerManagementJobProcessor.Stop();
        }

        public bool Handle(TransportMessage message)
        {
            this.ActiveMqSchedulerManagementJobProcessor.HandleTransportMessage(message);
            return true;
        }

        private void RunDeferredMessageCleanup(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                this.ActiveMqSchedulerManagementJobProcessor.ProcessAllJobs(token);
                Thread.Sleep(100);
            }
        }
    }
}