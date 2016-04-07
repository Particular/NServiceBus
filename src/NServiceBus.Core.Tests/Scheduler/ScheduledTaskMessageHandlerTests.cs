namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class ScheduledTaskMessageHandlerTests
    {
        TestableMessageHandlerContext handlingContext = new TestableMessageHandlerContext();
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

            var deferredMessage = handlingContext.SentMessages.First(message => message.Options.GetDeliveryDelay().HasValue).Message<ScheduledTask>();
            Assert.That(deferredMessage.TaskId, Is.EqualTo(taskId));
        }
    }
}