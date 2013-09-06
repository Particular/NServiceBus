namespace NServiceBus.Transports.ActiveMQ.Tests
{
    using System;
    using System.Threading;
    using Apache.NMS;
    using Moq;
    using NUnit.Framework;

    public class ActiveMqSchedulerManagementJobProcessorTests
    {
        private ActiveMqSchedulerManagementJobProcessor testee;
        private Mock<IActiveMqSchedulerManagementCommands> activeMqSchedulerManagementCommandsMock;

        [SetUp]
        public void SetUp()
        {
            activeMqSchedulerManagementCommandsMock = new Mock<IActiveMqSchedulerManagementCommands>();

            testee = new ActiveMqSchedulerManagementJobProcessor(activeMqSchedulerManagementCommandsMock.Object);
        }

        [Test]
        public void WhenStarted_ActiveMqSchedulerManagementCommandsAreStarted()
        {
            testee.Start();

            activeMqSchedulerManagementCommandsMock.Verify(c => c.Start());
        }

        [Test]
        public void WhenStopped_ActiveMqSchedulerManagementCommandsAreStopped()
        {
            testee.Stop();

            activeMqSchedulerManagementCommandsMock.Verify(c => c.Stop());
        }

        [Test]
        public void WhenMessageIsHandled_DeferredMessagesAreRequested()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector))
                .Returns(new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now));

            testee.HandleTransportMessage(message);

            activeMqSchedulerManagementCommandsMock.Verify(c => c.RequestDeferredMessages(destination));
        }

        [Test]
        public void WhenProcessingJobs_AllCurrentJobsAreProcessed()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            var job = new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now + TimeSpan.FromMinutes(1));
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector)).Returns(job);

            testee.HandleTransportMessage(message);
            testee.ProcessAllJobs(new CancellationToken(false));

            activeMqSchedulerManagementCommandsMock.Verify(c => c.ProcessJob(job));
            activeMqSchedulerManagementCommandsMock.Verify(c => c.DisposeJob(job), Times.Never());
        }

        [Test]
        public void WhenProcessingJobs_ExpiredJobsAreDisposed()
        {
            const string Selector = "Selector";
            var message = new TransportMessage();
            var destination = new Mock<IDestination>().Object;
            var job = new ActiveMqSchedulerManagementJob(null, destination, DateTime.Now + TimeSpan.FromMinutes(-1));
            message.Headers[ActiveMqSchedulerManagement.ClearScheduledMessagesSelectorHeader] = Selector;

            activeMqSchedulerManagementCommandsMock.Setup(c => c.CreateActiveMqSchedulerManagementJob(Selector)).Returns(job);

            testee.HandleTransportMessage(message);
            testee.ProcessAllJobs(new CancellationToken(false));

            activeMqSchedulerManagementCommandsMock.Verify(c => c.DisposeJob(job));
        }
    }
}