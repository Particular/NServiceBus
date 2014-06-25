namespace NServiceBus.Scheduling.Tests
{
    using System;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduledTaskMessageHandlerTests
    {
        FakeBus _bus = new FakeBus();
        InMemoryScheduledTaskStorage _taskStorage = new InMemoryScheduledTaskStorage();
        DefaultScheduler _scheduler;
        ScheduledTaskMessageHandler _handler;
        Guid _taskId;

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