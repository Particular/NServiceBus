namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    static class TaskExtensions
    {
        //this method will not timeout a task if the debugger is attached.
        public static async Task Timebox(this IEnumerable<Task> tasks, TimeSpan timeoutAfter, string messageWhenTimeboxReached)
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var delayTask = Debugger.IsAttached ? Task.Delay(-1, tokenSource.Token) : Task.Delay(timeoutAfter, tokenSource.Token);
                var allTasks = Task.WhenAll(tasks);

                var returnedTask = await Task.WhenAny(delayTask, allTasks).ConfigureAwait(false);
                tokenSource.Cancel();
                if (returnedTask == delayTask)
                {
                    throw new TimeoutException(messageWhenTimeboxReached);
                }

                await allTasks.ConfigureAwait(false);
            }
        }
    }
}