namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using ObjectBuilder;
    using Scheduling;

    /// <summary>
    /// Scheduling capability to schedule a task (as an <see cref="System.Action"/>) to be executed repeatedly in a given interval.
    /// </summary>
    /// <remarks>This is a in-memory list of <see cref="System.Action"/>s.</remarks>
    public class Schedule
    {
        IBuilder builder;

        /// <summary>
        /// Builds a new instance of <see cref="Schedule"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IBuilder"/>.</param>
        public Schedule(IBuilder builder)
        {
            Guard.AgainstNull(builder, "builder");
            this.builder = builder;
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        public void Every(TimeSpan timeSpan, Action task)
        {
            Guard.AgainstNull(task, "task");
            Guard.AgainstNegativeAndZero(timeSpan, "timeSpan");
            var declaringType = task.Method.DeclaringType;

            while (declaringType.DeclaringType != null &&
                declaringType.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute"))
            {
                declaringType = declaringType.DeclaringType;
            }

            Every(timeSpan, declaringType.Name, task);
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        /// <param name="name">The name to use used for logging inside the new <see cref="Thread"/>.</param>
        public void Every(TimeSpan timeSpan, string name, Action task)
        {
            Guard.AgainstNull(task, "task");
            Guard.AgainstNullAndEmpty(name, "name");
            Guard.AgainstNegativeAndZero(timeSpan, "timeSpan");
            builder.Build<DefaultScheduler>()
                .Schedule(new TaskDefinition
                {
                    Every = timeSpan,
                    Name = name,
                    Task = task
                });
        }
    }
}