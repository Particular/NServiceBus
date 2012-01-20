namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;


    /// <summary>
    /// The default dispatch factory
    /// </summary>
    public class DefaultDispatcherFactory : IMessageDispatcherFactory
    {
        public IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object toHandle)
        {
            yield return () => builder.BuildAndDispatch(messageHandlerType, h =>
                                                                          {
                                                                              var methodInfo = messageHandlerType.GetMethod("Handle", new[] { toHandle.GetType() });
                                                                              methodInfo.Invoke(h, new[] { toHandle });
                                                                          });
        }

        public bool CanDispatch(Type handler)
        {
            return true;
        }
    }
}