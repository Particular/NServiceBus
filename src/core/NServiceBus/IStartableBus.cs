using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjectBuilder;

namespace NServiceBus
{
    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    public interface IStartableBus : IDisposable
    {		
        /// <summary>
        /// Performs all the given startup actions, starts the bus, and returns a reference to it.
        /// </summary>
        /// <param name="startupActions">Actions to be performed before the bus is started.</param>
        /// <returns>A reference to the bus.</returns>
        IBus Start(params Action<IBuilder>[] startupActions);
    }
}
