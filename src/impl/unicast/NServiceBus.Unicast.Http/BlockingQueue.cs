using System;
using System.Collections;
using System.Threading;

namespace NServiceBus.Unicast.Transport.Http
{
    /// <summary>
    /// Same as Queue except Dequeue function blocks until there is an object to return.
    /// Note: This class does not need to be synchronized
    /// </summary>
    public class BlockingQueue : Queue
    {
        private bool open;

        /// <summary>
        /// Create new BlockingQueue.
        /// </summary>
        /// <param name="col">The System.Collections.ICollection to copy elements from</param>
        public BlockingQueue(ICollection col)
            : base(col)
        {

            open = true;

        }



        /// <summary>

        /// Create new BlockingQueue.

        /// </summary>

        /// <param name="capacity">The initial number of elements that the queue can contain</param>

        /// <param name="growFactor">The factor by which the capacity of the queue is expanded</param>

        public BlockingQueue(int capacity, float growFactor)
            : base(capacity, growFactor)
        {

            open = true;

        }



        /// <summary>

        /// Create new BlockingQueue.

        /// </summary>

        /// <param name="capacity">The initial number of elements that the queue can contain</param>

        public BlockingQueue(int capacity)
            : base(capacity)
        {

            open = true;

        }



        /// <summary>

        /// Create new BlockingQueue.

        /// </summary>

        public BlockingQueue()
        {

            open = true;

        }



        /// <summary>

        /// BlockingQueue Destructor (Close queue, resume any waiting thread).

        /// </summary>

        ~BlockingQueue()
        {

            Close();

        }



        /// <summary>

        /// Remove all objects from the Queue.

        /// </summary>

        public override void Clear()
        {

            lock (base.SyncRoot)
            {

                base.Clear();

            }

        }



        /// <summary>

        /// Remove all objects from the Queue, resume all dequeue threads.

        /// </summary>

        public void Close()
        {

            lock (base.SyncRoot)
            {

                open = false;

                base.Clear();

                Monitor.PulseAll(base.SyncRoot);    // resume any waiting threads

            }

        }



        /// <summary>

        /// Removes and returns the object at the beginning of the Queue.

        /// </summary>

        /// <returns>Object in queue.</returns>

        public override object Dequeue()
        {

            return Dequeue(Timeout.Infinite);

        }



        /// <summary>

        /// Removes and returns the object at the beginning of the Queue.

        /// </summary>

        /// <param name="timeout">time to wait before returning</param>

        /// <returns>Object in queue.</returns>

        public object Dequeue(TimeSpan timeout)
        {

            return Dequeue(timeout.Milliseconds);

        }



        /// <summary>

        /// Removes and returns the object at the beginning of the Queue.

        /// </summary>

        /// <param name="timeout">time to wait before returning (in milliseconds)</param>

        /// <returns>Object in queue.</returns>

        public object Dequeue(int timeout)
        {

            lock (base.SyncRoot)
            {

                while (open && (base.Count == 0))
                {

                    if (!Monitor.Wait(base.SyncRoot, timeout))

                        throw new InvalidOperationException("Timeout");

                }

                if (open)

                    return base.Dequeue();

                else

                    throw new InvalidOperationException("Queue Closed");

            }

        }



        /// <summary>

        /// Adds an object to the end of the Queue.

        /// </summary>

        /// <param name="obj">Object to put in queue</param>

        public override void Enqueue(object obj)
        {

            lock (base.SyncRoot)
            {

                base.Enqueue(obj);

                Monitor.Pulse(base.SyncRoot);

            }

        }



        /// <summary>

        /// Open Queue.

        /// </summary>

        public void Open()
        {

            lock (base.SyncRoot)
            {

                open = true;

            }

        }



        /// <summary>

        /// Gets flag indicating if queue has been closed.

        /// </summary>

        public bool Closed
        {

            get
            {

                return !open;

            }

        }

    }

}
