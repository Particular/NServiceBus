using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus
{
    public interface IStartableBus : IDisposable
    {		
        /// <summary>
        /// Starts the bus.
        /// </summary>
        void Start();
    }
}
