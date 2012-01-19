namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;
    using Saga;

    public class DefaultDispatcherFactory : IMessageDispatcherFactory
    {
        public IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object toHandle)
        {
            //todo: Refactor this
            if (typeof(ISaga).IsAssignableFrom(messageHandlerType))
                yield break;

            yield return () => builder.BuildAndDispatch(messageHandlerType, h =>
                                                                          {
                                                                              var methodInfo = messageHandlerType.GetMethod("Handle", new[] { toHandle.GetType() });
                                                                              methodInfo.Invoke(h, new[] { toHandle });
                                                                          });
        }
    }
}