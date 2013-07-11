namespace NServiceBus.Scheduling.Tests
{
    using System.Threading;
    using NUnit.Framework;
    using Core.Tests.Fakes;

    [TestFixture]
    public class DefaultSchedulerTests
    {
        private FakeBus _bus = new FakeBus();
        private readonly IScheduledTaskStorage _taskStorage = new InMemoryScheduledTaskStorage();
        private IScheduler _scheduler;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new DefaultScheduler(_bus, _taskStorage);
        }

        [Test]
        public void When_scheduling_a_task_it_should_be_added_to_the_storage()
        {
            var task = new ScheduledTask();
            var taskId = task.Id;
            _scheduler.Schedule(task);

            Assert.That(_taskStorage.Get(taskId).Id, Is.EqualTo(taskId));
        }

        [Test]
        public void When_scheduling_a_task_defer_should_be_called()
        {
            _scheduler.Schedule(new ScheduledTask());
            Assert.That(_bus.DeferWasCalled > 0);
        }

        [Test]
        public void When_starting_a_task_defer_should_be_called()
        {
            var task = new ScheduledTask {Task = () => { }};
            var taskId = task.Id;

            _scheduler.Schedule(task);

            var deferCount = _bus.DeferWasCalled;
            _scheduler.Start(taskId);
            
            Assert.That(_bus.DeferWasCalled > deferCount);
        }

        [Test]
        public void When_starting_a_task_the_lambda_should_be_executed()
        {
            var i = 1;

            var task = new ScheduledTask { Task = () => { i++; } };
            var taskId = task.Id;

            _scheduler.Schedule(task);            
            _scheduler.Start(taskId);

            Thread.Sleep(100); // Wait for the task...

            Assert.That(i == 2);
        }
    }
}
