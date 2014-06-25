namespace NServiceBus.Scheduling.Tests
{
    using System;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduledTaskMessageHandlerTests
    {
        private FakeBus _bus = new FakeBus();
        private InMemoryScheduledTaskStorage _taskStorage = new InMemoryScheduledTaskStorage();
        private IScheduler _scheduler;
        private ScheduledTaskMessageHandler _handler;

        private Guid _taskId;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new DefaultScheduler(_bus, _taskStorage);
            _handler = new ScheduledTaskMessageHandler(_scheduler);

            var task = new ScheduledTask{Task = () => { }};
            _taskId = task.Id;
            _scheduler.Schedule(task);
        }

        [Test]
        public void When_a_scheduledTask_message_is_handled_the_task_should_be_defer()
        {
            _handler.Handle(new Messages.ScheduledTask{TaskId = _taskId});
            Assert.That(((Messages.ScheduledTask)_bus.DeferMessages[0]).TaskId, Is.EqualTo(_taskId));
        }
    }
}