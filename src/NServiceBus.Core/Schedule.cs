namespace NServiceBus
{
    using System;
    using System.Threading;
    using ObjectBuilder;
    using Scheduling;

    /// <summary>
    /// Scheduling capability to schedule a task (as an <see cref="System.Action"/>) to be executed repeatedly in a given interval.
    /// </summary>
    /// <remarks>This is a in-memory list of <see cref="System.Action"/>s.</remarks>
    public partial class Schedule
    {
        IBuilder builder;

        /// <summary>
        /// Builds a new instance of <see cref="Schedule"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IBuilder"/>.</param>
        public Schedule(IBuilder builder)
        {
            this.builder = builder;
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        public void Every(TimeSpan timeSpan, Action task)
        {
            Every(timeSpan, task.Method.DeclaringType.Name, task);
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        /// <param name="name">The name to use to when creating the <see cref="Thread"/>.</param>
        public void Every(TimeSpan timeSpan, string name, Action task)
        {
            builder.Build<IScheduler>().Schedule(new ScheduledTask { Every = timeSpan, Name = name, Task = task });
        }

    }
}
