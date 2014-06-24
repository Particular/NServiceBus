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
        private const string ACTION_NAME = "my action";
        private FuncBuilder _builder = new FuncBuilder();
        private FakeBus _bus = new FakeBus();
        Schedule schedule;

        private readonly InMemoryScheduledTaskStorage _taskStorage = new InMemoryScheduledTaskStorage();        

        [SetUp]
        public void SetUp()
        {
            Configure.With(o=>o.AssembliesToScan(new Assembly[0]).UseContainer(_builder));

            _builder.Register<IBus>(() => _bus);
            _builder.Register<IScheduledTaskStorage>(() => _taskStorage);
            _builder.Register<IScheduler>(() => new DefaultScheduler(_bus, _taskStorage));

            schedule = new Schedule(_builder);
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
            
            _bus.DeferWasCalled = 0;

            Parallel.ForEach(_taskStorage.Tasks,
                              t => new ScheduledTaskMessageHandler(new DefaultScheduler(_bus, _taskStorage)).Handle(
                                  new Messages.ScheduledTask { TaskId = t.Key }));

            Assert.That(_bus.DeferWasCalled, Is.EqualTo(20));
        }

        private bool EnsureThatNameExists(string name)
        {
            return _taskStorage.Tasks.Any(task => task.Value.Name.Equals(name));
        }
    }
}