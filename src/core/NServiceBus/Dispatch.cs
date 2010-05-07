using System;

namespace NServiceBus
{
    public class IHandle<M> where M : IMessage
    {
        public interface DispatchingToEntity<E> : IHandleMessages<M> {}
    }

    public static class _Dispatcher
    {
        public static void TransactionallyDispatch<ID, M, E>(this IHandle<M>.DispatchingToEntity<E> handler, Func<M, ID> getIdFromMessage, Action<E> callEntity) where M : class, IMessage  where E : class, new()
        {
            var m = ExtensionMethods.CurrentMessageBeingHandled as M;
            var id = getIdFromMessage(m);
            GetAndCallEntity(id, typeof(E), o => callEntity(o as E));
        }

        public static Action<object, Type, Action<object>> GetAndCallEntity { get; set; }
    }
}
