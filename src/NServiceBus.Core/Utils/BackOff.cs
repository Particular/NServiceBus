namespace NServiceBus.Utils
{
    using System;
    using System.Threading;

    /// <summary>
    /// A utility class that does a sleep on very call up to a limit based on a condition.
    /// </summary>
    public class BackOff
    {
        private readonly int maximum;
        private int currentDelay = 50;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="maximum">The maximum number of milliseconds for which the thread is blocked.</param>
        public BackOff(int maximum)
        {
            this.maximum = maximum;
        }

        /// <summary>
        /// It executes the Thread sleep if condition is <c>true</c>, otherwise it resets.
        /// </summary>
        /// <param name="condition">If the condition is <c>true</c> then the wait is performed.</param>
        public void Wait(Func<bool> condition)
        {
            if (!condition())
            {
                currentDelay = 50;
                return;
            }

            Thread.Sleep(currentDelay);
            
            if (currentDelay < maximum)
            {
                currentDelay *= 2;
            }

            if (currentDelay > maximum)
            {
                currentDelay = maximum;                
            }
        }
    }
}
