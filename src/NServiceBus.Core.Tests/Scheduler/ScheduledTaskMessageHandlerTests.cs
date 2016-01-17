namespace NServiceBus.Scheduling.Tests
{
    using System;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduledTaskMessageHandlerTests
    {
        FakeHandlingContext handlingContext = new FakeHandlingContext();
        DefaultScheduler scheduler;
        ScheduledTaskMessageHandler handler;
        Guid taskId;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            handler = new ScheduledTaskMessageHandler(scheduler);

            var task = new TaskDefinition
            {
                Every = TimeSpan.FromSeconds(5),
                Task = c => TaskEx.CompletedTask
            };
            taskId = task.Id;
            scheduler.Schedule(task);
        }

        [Test]
        public void When_a_scheduledTask_message_is_handled_the_task_should_be_defer()
        {
            handler.Handle(new ScheduledTask
            {
                Every = TimeSpan.FromSeconds(5),
                TaskId = taskId
            }, handlingContext);

            Assert.That(((ScheduledTask)handlingContext.DeferedMessage).TaskId, Is.EqualTo(taskId));
        }
    }
}