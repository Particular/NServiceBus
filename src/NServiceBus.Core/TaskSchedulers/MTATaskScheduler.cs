//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: StaTaskScheduler.cs
//
//--------------------------------------------------------------------------

namespace System.Threading.Tasks.Schedulers
{
    using Collections.Concurrent;
    using Collections.Generic;
    using Janitor;
    using Linq;

    /// <summary>Provides a scheduler that uses MTA threads.</summary>
    [SkipWeaving]
    public sealed class MTATaskScheduler : TaskScheduler, IDisposable
    {
        private bool disposed;
        /// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
        private BlockingCollection<Task> tasks;
        /// <summary>The MTA threads used by the scheduler.</summary>
        private readonly List<Thread> _threads;

        /// <summary>Initializes a new instance of the MTATaskScheduler class with the specified concurrency level.</summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        /// <param name="nameFormat">The template name form to use to name threads.</param>
        public MTATaskScheduler(int numberOfThreads, string nameFormat)
        {
            // Validate arguments
            if (numberOfThreads < 1) throw new ArgumentOutOfRangeException("numberOfThreads");

            // Initialize the tasks collection
            tasks = new BlockingCollection<Task>();

            // Create the threads to be used by this scheduler
            _threads = Enumerable.Range(0, numberOfThreads).Select(i =>
                       {
                           var thread = new Thread(() =>
                           {
                               // Continually get the next task and try to execute it.
                               // This will continue until the scheduler is disposed and no more tasks remain.
                               foreach (var t in tasks.GetConsumingEnumerable())
                               {
                                   TryExecuteTask(t);
                               }
                           });
                           thread.IsBackground = true;
                           thread.SetApartmentState(ApartmentState.MTA);
                           thread.Name = String.Format("{0} - {1}", nameFormat, thread.ManagedThreadId);
                           return thread;
                       }).ToList();

            // Start all of the threads
            _threads.ForEach(t => t.Start());
        }

        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            // Push it into the blocking collection of tasks
            tasks.Add(task);
        }

        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            // Serialize the contents of the blocking collection of tasks for the debugger
            return tasks.ToArray();
        }

        /// <summary>Determines whether a Task may be inlined.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel
        {
            get { return _threads.Count; }
        }

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (tasks != null)
            {
                // Indicate that no new tasks will be coming in
                tasks.CompleteAdding();

                // Wait for all threads to finish processing tasks
                foreach (var thread in _threads)
                {
                    thread.Join();
                }

                // Cleanup
                tasks.Dispose();
                tasks = null;
            }

        }
    }
}