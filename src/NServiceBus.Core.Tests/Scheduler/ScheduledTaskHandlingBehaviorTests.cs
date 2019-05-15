namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Pipeline;
    using Testing;
    using Unicast.Messages;

    [TestFixture]
    public class ScheduledTaskHandlingBehaviorTests
    {
        [SetUp]
        public void SetUp()
        {
            logicalContext = new TestableIncomingLogicalMessageContext();
            scheduler = new DefaultScheduler();
            behavior = new ScheduledTaskHandlingBehavior(scheduler);

            var task = new TaskDefinition
            {
                Every = TimeSpan.FromSeconds(5),
                Task = c => TaskEx.CompletedTask
            };
            taskId = task.Id;
            scheduler.Schedule(task);
        }

        [Test]
        public async Task When_a_scheduledTask_message_is_handled_the_task_should_be_deferred()
        {
            logicalContext.Message = new LogicalMessage(new MessageMetadata(typeof(ScheduledTask)), new ScheduledTask
            {
                Every = TimeSpan.FromSeconds(5),
                TaskId = taskId
            });

            var nextWasCalled = false;
            await behavior.Invoke(logicalContext, ctx =>
            {
                nextWasCalled = true;
                return Task.FromResult(0);
            });

            var deferredMessage = logicalContext.SentMessages.First(message => message.Options.GetDeliveryDelay().HasValue).Message<ScheduledTask>();
            Assert.That(deferredMessage.TaskId, Is.EqualTo(taskId));
            Assert.True(nextWasCalled, "Next should have been called");
        }

        [Test]
        public async Task When_another_message_is_handled_no_task_should_not_be_deferred()
        {
            logicalContext.Message = new LogicalMessage(new MessageMetadata(typeof(object)), new object());

            var nextWasCalled = false;
            await behavior.Invoke(logicalContext, ctx =>
            {
                nextWasCalled = true;
                return Task.FromResult(0);
            });

            Assert.IsEmpty(logicalContext.SentMessages, "Nothing should have been deferred");
            Assert.True(nextWasCalled, "Next should have been called");
        }

        TestableIncomingLogicalMessageContext logicalContext;
        DefaultScheduler scheduler;
        ScheduledTaskHandlingBehavior behavior;
        Guid taskId;
    }
}