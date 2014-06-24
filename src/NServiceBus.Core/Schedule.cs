namespace NServiceBus
{
    using System;
    using ObjectBuilder;
    using Scheduling;

    /// <summary>
    /// Scheduling capability to schedule a task or an action/lambda, to be executed repeatedly in a given interval.
    /// </summary>
    public class Schedule
    {
        readonly IBuilder builder;

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
        /// <param name="task">The task to execute.</param>
        public void Every(TimeSpan timeSpan, Action task)
        {
            Every(timeSpan, task.Method.DeclaringType.Name, task);
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The task to execute.</param>
        /// <param name="name">The name to use to when creating the thread.</param>
        public void Every(TimeSpan timeSpan, string name, Action task)
        {
            builder.Build<IScheduler>().Schedule(new ScheduledTask { Every = timeSpan, Name = name, Task = task });
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="timeSpan">The interval to repeatedly execute the task.</param>
        /// <returns>A <see cref="Schedule"/> instance.</returns>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "Inject Schedule")]
// ReSharper disable UnusedParameter.Global
        public static Schedule Every(TimeSpan timeSpan)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException("Api has been obsolete.");
        }

        /// <summary>
        /// The action to execute.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Replacement = "Schedule.Every(TimeSpan timeSpan, Action task)")]
// ReSharper disable UnusedParameter.Global
        public void Action(Action task)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException("Api has been obsolete.");
        }

        /// <summary>
        /// The action to execute.
        /// </summary>
        /// <param name="task">The task to execute.</param>
        /// <param name="name">The name to use to when creating the thread.</param>
        [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Replacement = "Schedule.Every(TimeSpan timeSpan, string name, Action task)")]
// ReSharper disable UnusedParameter.Global
        public void Action(string name, Action task)
// ReSharper restore UnusedParameter.Global
        {
            throw new NotImplementedException("Api has been obsolete.");       
        }
    }
}
