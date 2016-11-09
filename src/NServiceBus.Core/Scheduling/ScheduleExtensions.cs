namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    /// <summary>
    /// Extends the context with scheduling capabilities.
    /// </summary>
    public static class ScheduleExtensions
    {
        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="session">The session which allows you to perform message operation.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task" />.</param>
        /// <param name="task">The <see cref="System.Action" /> to execute.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, Func<IPipelineContext, Task> task)", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public static void ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, Action task)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="session">The session which allows you to perform message operation.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task" />.</param>
        /// <param name="task">The <see cref="System.Action" /> to execute.</param>
        /// <param name="name">The name to use used for logging inside the new <see cref="Thread" />.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, string name, Func<IPipelineContext, Task> task)", TreatAsErrorFromVersion = "6", RemoveInVersion = "7")]
        public static void ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, string name, Action task)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="session">The session which allows you to perform message operation.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task" />.</param>
        /// <param name="task">The async function to execute.</param>
        public static Task ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, Func<IPipelineContext, Task> task)
        {
            Guard.AgainstNull(nameof(task), task);
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);

            var declaringType = task.Method.DeclaringType;
            while (declaringType.DeclaringType != null &&
                   declaringType.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute"))
            {
                declaringType = declaringType.DeclaringType;
            }

            return ScheduleEvery(session, timeSpan, declaringType.Name, task);
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="session">The session which allows you to perform message operation.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task" />.</param>
        /// <param name="task">The async function to execute.</param>
        /// <param name="name">The name to used for logging the task being executed.</param>
        public static Task ScheduleEvery(this IMessageSession session, TimeSpan timeSpan, string name, Func<IPipelineContext, Task> task)
        {
            Guard.AgainstNull(nameof(task), task);
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);

            var taskDefinition = new TaskDefinition
            {
                Every = timeSpan,
                Name = name,
                Task = task
            };
            return Schedule(session, taskDefinition);
        }

        static Task Schedule(IMessageSession session, TaskDefinition taskDefinition)
        {
            logger.DebugFormat("Task '{0}' (with id {1}) scheduled with timeSpan {2}", taskDefinition.Name, taskDefinition.Id, taskDefinition.Every);

            var options = new SendOptions();
            options.DelayDeliveryWith(taskDefinition.Every);
            options.RouteToThisEndpoint();
            options.Context.GetOrCreate<ScheduleBehavior.State>().TaskDefinition = taskDefinition;

            var scheduledTask = new ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            };
            return session.Send(scheduledTask, options);
        }

        static ILog logger = LogManager.GetLogger<DefaultScheduler>(); //Intentionally using different type.
    }
}