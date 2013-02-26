﻿namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class ActiveMqSchedulerManagementJobProcessor
    {
        private IActiveMqSchedulerManagementCommands activeMqSchedulerManagementCommands;
        private readonly ConcurrentDictionary<ActiveMqSchedulerManagementJob, ActiveMqSchedulerManagementJob> jobs = 
            new ConcurrentDictionary<ActiveMqSchedulerManagementJob, ActiveMqSchedulerManagementJob>();

        public ActiveMqSchedulerManagementJobProcessor(IActiveMqSchedulerManagementCommands activeMqSchedulerManagementCommands)
        {
            this.activeMqSchedulerManagementCommands = activeMqSchedulerManagementCommands;
        }

        public void Start()
        {
            this.activeMqSchedulerManagementCommands.Start();
        }

        public void Stop()
        {
            this.DeleteJobs(this.jobs.Keys);
            this.activeMqSchedulerManagementCommands.Stop();
        }
        
        public bool HandleTransportMessage(TransportMessage message)
        {
            var job = this.activeMqSchedulerManagementCommands.CreateActiveMqSchedulerManagementJob(
                message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader]);

            this.activeMqSchedulerManagementCommands.RequestDeferredMessages(job.Destination);
            this.jobs[job] = job;

            return true;
        }

        public void ProcessAllJobs(CancellationToken token)
        {
            foreach (var job in this.jobs.Keys.ToList())
            {
                token.ThrowIfCancellationRequested();
                this.activeMqSchedulerManagementCommands.ProcessJob(job);
            }

            this.RemoveExpiredJobs();
        }

        private void RemoveExpiredJobs()
        {
            this.DeleteJobs(this.jobs.Keys.Where(j => DateTime.Now > j.ExprirationDate));
        }

        private void DeleteJobs(IEnumerable<ActiveMqSchedulerManagementJob> jobs)
        {
            foreach (var job in jobs.ToList())
            {
                ActiveMqSchedulerManagementJob jobValue;
                this.jobs.TryRemove(job, out jobValue);
                this.activeMqSchedulerManagementCommands.DisposeJob(job);
            }
        }
    }
}