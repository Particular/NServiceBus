namespace NServiceBus.Scheduling.Tests
{
    using System;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduledTaskMessageHandlerTests
    {
        FakeBus bus = new FakeBus();
        DefaultScheduler scheduler;
        ScheduledTaskMessageHandler handler;
        Guid taskId;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler(bus);
            handler = new ScheduledTaskMessageHandler(scheduler);

            var task = new TaskDefinition{Task = () => { }};
            taskId = task.Id;
            scheduler.Schedule(task);
        }

        [Test]
        public void When_a_scheduledTask_message_is_handled_the_task_should_be_defer()
        {
            handler.Handle(new Messages.ScheduledTask{TaskId = taskId});
            Assert.That(((Messages.ScheduledTask)bus.DeferMessages[0]).TaskId, Is.EqualTo(taskId));
        }
    }
}