namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduleTests
    {
        const string ACTION_NAME = "my action";
        FakeBus bus;
        DefaultScheduler scheduler;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            bus = new FakeBus(scheduler);
        }

        [Test]
        public void When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            bus.ScheduleEvery(TimeSpan.FromMinutes(5), ACTION_NAME, () => { });
            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public void When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            bus.ScheduleEvery(TimeSpan.FromMinutes(5), () => { });
            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        [Test]
        public void Ensure_retrieving_name_from_type_works_for_old_compiler()
        {
            bus.ScheduleEvery(TimeSpan.FromMinutes(5), OldCompilerBits.ActionProvider.SimpleAction());
            Assert.That(EnsureThatNameExists("ActionProvider"));
        }

        [Test]
        public void Ensure_retrieving_name_from_type_works_for_new_compiler()
        {
            bus.ScheduleEvery(TimeSpan.FromMinutes(5), NewCompilerBits.ActionProvider.SimpleAction());
            Assert.That(EnsureThatNameExists("ActionProvider"));
        }

        bool EnsureThatNameExists(string name)
        {
            return scheduler.scheduledTasks.Any(task => task.Value.Name.Equals(name));
        }

        private class FakeBus : ISendOnlyBus
        {
            readonly DefaultScheduler defaultScheduler;
            public readonly List<Tuple<object, SendOptions>> SentMessages = new List<Tuple<object, SendOptions>>();

            public FakeBus(DefaultScheduler defaultScheduler)
            {
                this.defaultScheduler = defaultScheduler;
            }

            public void Dispose()
            {
            }

            public Task PublishAsync(object message, PublishOptions options)
            {
                throw new NotImplementedException();
            }

            public Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
            {
                throw new NotImplementedException();
            }

            public Task SendAsync(object message, SendOptions options)
            {
                SentMessages.Add(Tuple.Create(message, options));
                defaultScheduler.Schedule(options.Context.Get<ScheduleBehavior.State>().TaskDefinition);
                return Task.FromResult(0);
            }

            public Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
            {
                throw new NotImplementedException();
            }

            public Task SubscribeAsync(Type eventType, SubscribeOptions options)
            {
                throw new NotImplementedException();
            }

            public Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}