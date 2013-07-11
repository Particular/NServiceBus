namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Threading;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;
    using NServiceBus.Transports.ActiveMQ;

    public class ActiveMqSchedulerManagementJobProcessorTests
    {
        private ActiveMqSchedulerManagementJobProcessor testee;
        private Mock<IActiveMqSchedulerManagementCommands> activeMqSchedulerManagementCommandsMock;

        [SetUp]
        public void SetUp()
        {
            this.activeMqSchedulerManagementCommandsMock = new Mock<IActiveMqSchedulerManagementCommands>();

            this.testee = new ActiveMqSchedulerManagementJobProcessor(this.activeMqSchedulerManagementCommandsMock.Object);
        }

        [Test]
        public void WhenStarted_ActiveMqSchedulerManagementCommandsAreStarted()
        {
            this.testee.Start();

            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.Start());
        }

        [Test]
        public void WhenStopped_ActiveMqSchedulerManagementCommandsAreStopped()
        {
            this.testee.Stop();

            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.Stop());
        }

        [Test]
        public void WhenMessageIsHandled_DeferredMessagesAreRequested()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            this.activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector))
                .Returns(new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now));

            this.testee.HandleTransportMessage(message);

            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.RequestDeferredMessages(destination));
        }

        [Test]
        public void WhenProcessingJobs_AllCurrentJobsAreProcessed()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            var job = new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now + TimeSpan.FromMinutes(1));
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            this.activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector)).Returns(job);

            this.testee.HandleTransportMessage(message);
            this.testee.ProcessAllJobs(new CancellationToken(false));

            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.ProcessJob(job));
            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.DisposeJob(job), Times.Never());
        }

        [Test]
        public void WhenProcessingJobs_ExpiredJobsAreDisposed()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            var job = new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now + TimeSpan.FromMinutes(-1));
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            this.activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector)).Returns(job);

            this.testee.HandleTransportMessage(message);
            this.testee.ProcessAllJobs(new CancellationToken(false));

            this.activeMqSchedulerManagementCommandsMock.Verify(c => c.DisposeJob(job));
        }
    }
}