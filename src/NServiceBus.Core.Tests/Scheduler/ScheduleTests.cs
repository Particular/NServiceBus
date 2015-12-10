namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduleTests
    {
        const string ACTION_NAME = "my action";
        DefaultScheduler scheduler;
        IBusSession context;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            context = new FakeBusSession(scheduler);
        }

        [Test]
        public async Task When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            await context.ScheduleEvery(TimeSpan.FromMinutes(5), ACTION_NAME, c => TaskEx.Completed);

            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public async Task When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            await context.ScheduleEvery(TimeSpan.FromMinutes(5), c => TaskEx.Completed);

            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        bool EnsureThatNameExists(string name)
        {
            return scheduler.scheduledTasks.Any(task => task.Value.Name.Equals(name));
        }

        class FakeBusSession : IBusSession
        {
            readonly DefaultScheduler defaultScheduler;

            public FakeBusSession(DefaultScheduler defaultScheduler)
            {
                this.defaultScheduler = defaultScheduler;
            }

            public Task Send(object message, SendOptions options)
            {
                defaultScheduler.Schedule(options.Context.Get<ScheduleBehavior.State>().TaskDefinition);
                return Task.FromResult(0);
            }

            public Task Send<T>(Action<T> messageConstructor, SendOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Publish(object message, PublishOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
            {
                throw new NotImplementedException();
            }

            public Task Subscribe(Type eventType, SubscribeOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}