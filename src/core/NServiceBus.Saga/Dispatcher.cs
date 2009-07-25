using System;
using System.Linq.Expressions;
using NServiceBus.Utils.Reflection;
using System.Reflection;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Double-dispatch class.
    /// </summary>
    public static class Dispatcher
    {
        /// <summary>
        /// Callback for when saga wants to configure which property of which message type
        /// should be used to look it up based on its given property.
        /// </summary>
        public static Action<Type, PropertyInfo, Type, PropertyInfo> CallbackWithSagaAndMessageProperties { get; set; }

        /// <summary>
        /// Callback for when saga is trying to reply to an originator that is null.
        /// </summary>
        public static Action CallbackWhenReplyingToNullOriginator;

        internal static void ConfigureHowToFindSagaWithMessage<TSaga, TMessage>(Expression<Func<TSaga, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TMessage : IMessage where TSaga : ISagaEntity
        {
            if (CallbackWithSagaAndMessageProperties == null) return;

            var sagaProp = Reflect<TSaga>.GetProperty(sagaEntityProperty);
            var messageProp = Reflect<TMessage>.GetProperty(messageProperty);

            CallbackWithSagaAndMessageProperties(typeof (TSaga), sagaProp, typeof (TMessage), messageProp);
        }

        internal static void TriedToReplyToNullOriginator()
        {
            if (CallbackWhenReplyingToNullOriginator != null)
                CallbackWhenReplyingToNullOriginator();
        }
    }
}
