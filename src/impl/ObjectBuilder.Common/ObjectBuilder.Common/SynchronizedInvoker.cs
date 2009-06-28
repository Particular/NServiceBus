using System;
using System.Runtime.Remoting.Contexts;

namespace NServiceBus.ObjectBuilder.Common
{
    /// <summary>
    /// Invokes methods and actions within a synchronization domain.
    /// </summary>
    [Synchronization(SynchronizationAttribute.REQUIRED)]
    public class SynchronizedInvoker
    {
        /// <summary>
        /// The container used to instantiate components.
        /// </summary>
        public IContainer Container { get; set; }

        /// <summary>
        /// Uses the container to create the given type and then calls the given
        /// action on the object created.
        /// </summary>
        /// <param name="typeToBuild"></param>
        /// <param name="action"></param>
        public void BuildAndDispatch(Type typeToBuild, Action<object> action)
        {
            if (Container == null)
                throw new InvalidOperationException("Cannot perform this action without a Container configured.");

            action(Container.Build(typeToBuild));
        }
    }
}
