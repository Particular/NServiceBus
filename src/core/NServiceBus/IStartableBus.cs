using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    public interface IStartableBus : IDisposable
    {		
        /// <summary>
        /// Starts the bus.
        /// </summary>
        IBus Start(params Action<IBuilder>[] startupActions);
    }
}
