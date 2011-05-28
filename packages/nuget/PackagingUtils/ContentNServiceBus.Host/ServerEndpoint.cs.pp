using System;
using NServiceBus;

namespace $rootnamespace$
{
    /// <summary>
    /// 
    /// </summary>
    public class ServerEndpoint : IWantToRunAtStartup
    {
        /// <summary>
        /// The ServiceBus
        /// </summary>
        public IBus Bus { get; set; }
		/// <summary>
		/// Normaly Start the bus here
		/// </summary>
        public void Run()
        {
            //if (Bus != null)
            //    Bus.Start();
            //What to do on 
        }

        /// <summary>
        /// OOPS Idont have bus stop 
        /// But write any logic here while service go for a sleep
        /// </summary>
        public void Stop()
        {

        }
    }
}