namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Core.Tests;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduleTests
    {
        const string ACTION_NAME = "my action";
        FuncBuilder builder = new FuncBuilder();
        FakeBus bus = new FakeBus();
        Schedule schedule;

        DefaultScheduler defaultScheduler ;
        [SetUp]
        public void SetUp()
        {
            Configure.With(o =>
            {
                o.AssembliesToScan(new Assembly[0]);
                o.UseContainer(builder);
            });

            defaultScheduler = new DefaultScheduler(bus);
            builder.Register<IBus>(() => bus);
            builder.Register<DefaultScheduler>(() => defaultScheduler);

            schedule = new Schedule(builder);
        }

        [Test]
        public void When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            schedule.Every(TimeSpan.FromMinutes(5), ACTION_NAME, () => { });
            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public void When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            schedule.Every(TimeSpan.FromMinutes(5), () => {  });
            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        [Test]
        public void Schedule_tasks_using_multiple_threads()
        {
            Parallel.For(0, 20, i => schedule.Every(TimeSpan.FromSeconds(1), () => { }));
            
            bus.DeferWasCalled = 0;

            Parallel.ForEach(defaultScheduler.scheduledTasks,
                              t => new ScheduledTaskMessageHandler(defaultScheduler).Handle(
                                  new Messages.ScheduledTask { TaskId = t.Key }));

            Assert.That(bus.DeferWasCalled, Is.EqualTo(20));
        }

        bool EnsureThatNameExists(string name)
        {
            return defaultScheduler.scheduledTasks.Any(task => task.Value.Name.Equals(name));
        }
    }
}