namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Logging;
    using Scheduling;

    /// <summary>
    /// Extends the bus with scheduling capabilities.
    /// </summary>
    public static class ScheduleBusExtensions
    {
        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="bus">Bus.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        public static void ScheduleEvery(this IBusContext bus, TimeSpan timeSpan, Action task)
        {
            Guard.AgainstNull(nameof(task), task);
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);
            var declaringType = task.Method.DeclaringType;

            while (declaringType.DeclaringType != null &&
                declaringType.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute"))
            {
                declaringType = declaringType.DeclaringType;
            }

            ScheduleEvery(bus, timeSpan, declaringType.Name, task);
        }

        /// <summary>
        /// Schedules a task to be executed repeatedly in a given interval.
        /// </summary>
        /// <param name="bus">Bus.</param>
        /// <param name="timeSpan">The interval to repeatedly execute the <paramref name="task"/>.</param>
        /// <param name="task">The <see cref="System.Action"/> to execute.</param>
        /// <param name="name">The name to use used for logging inside the new <see cref="Thread"/>.</param>
        public static void ScheduleEvery(this IBusContext bus, TimeSpan timeSpan, string name, Action task)
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
            Schedule(taskDefinition, bus);
        }

        static void Schedule(TaskDefinition taskDefinition, IBusContext context)
        {
            logger.DebugFormat("Task '{0}' (with id {1}) scheduled with timeSpan {2}", taskDefinition.Name, taskDefinition.Id, taskDefinition.Every);

            var options = new SendOptions();
            options.DelayDeliveryWith(taskDefinition.Every);
            options.RouteToLocalEndpointInstance();
            options.Context.GetOrCreate<ScheduleBehavior.State>().TaskDefinition = taskDefinition;

            context.SendAsync(new Scheduling.Messages.ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            }, options).GetAwaiter().GetResult();
        }

        static ILog logger = LogManager.GetLogger<DefaultScheduler>(); //Intentionally using different type.
    }

    
}