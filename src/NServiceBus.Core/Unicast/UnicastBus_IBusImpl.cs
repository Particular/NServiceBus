using System;

namespace NServiceBus.Unicast
{
    public partial class UnicastBus
    {
        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}()"/>
        /// </summary>
        /// <param name="message"></param>
        public void Publish(object message)
        {
            Guard.AgainstNull(message, "message");
            busImpl.Publish(message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}()"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Publish<T>()
        {
            busImpl.Publish<T>();
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}(Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        public void Publish<T>(Action<T> messageConstructor)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            busImpl.Publish(messageConstructor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public ICallback Send(object message,SendContext context)
        {
            return busImpl.Send(message, context);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public ICallback Send<T>(Action<T> messageConstructor, SendContext context)
        {
            return busImpl.Send(messageConstructor,context);
        }

        /// <summary>
        /// <see cref="IBus.Subscribe"/>
        /// </summary>
        /// <param name="messageType"></param>
        public void Subscribe(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            busImpl.Subscribe(messageType);
        }

        /// <summary>
        /// <see cref="IBus.Subscribe{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Subscribe<T>()
        {
            busImpl.Subscribe<T>();
        }

        /// <summary>
        /// <see cref="IBus.Unsubscribe"/>
        /// </summary>
        /// <param name="messageType"></param>
        public void Unsubscribe(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            busImpl.Unsubscribe(messageType);
        }

        /// <summary>
        /// <see cref="IBus.Unsubscribe{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Unsubscribe<T>()
        {
            busImpl.Unsubscribe<T>();
        }

        /// <summary>
        /// <see cref="IBus.SendLocal(object)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback SendLocal(object message)
        {
            Guard.AgainstNull(message, "message");
            return busImpl.SendLocal(message);
        }

        /// <summary>
        /// <see cref="IBus.SendLocal{T}(Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            return busImpl.SendLocal(messageConstructor);
        }

        /// <summary>
        /// <see cref="IBus.Defer(TimeSpan, object)"/>
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Defer(TimeSpan delay, object message)
        {
            Guard.AgainstNull(message, "message");
            return busImpl.Defer(delay, message);
        }

        /// <summary>
        /// <see cref="IBus.Defer(System.TimeSpan,object)"/>
        /// </summary>
        /// <param name="processAt"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Defer(DateTime processAt, object message)
        {
            Guard.AgainstNull(message, "message");
            return busImpl.Defer(processAt, message);
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        /// <param name="message"></param>
        public void Reply(object message)
        {
            Guard.AgainstNull(message, "message");
            busImpl.Reply(message);
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        public void Reply<T>(Action<T> messageConstructor)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            busImpl.Reply(messageConstructor);
        }

        /// <summary>
        /// <see cref="IBus.Return{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="errorEnum"></param>
        public void Return<T>(T errorEnum)
        {
            busImpl.Return(errorEnum);
        }

        /// <summary>
        /// <see cref="IBus.HandleCurrentMessageLater"/>
        /// </summary>
        public void HandleCurrentMessageLater()
        {
            busImpl.HandleCurrentMessageLater();
        }

        /// <summary>
        /// <see cref="IBus.ForwardCurrentMessageTo"/>
        /// </summary>
        /// <param name="destination"></param>
        public void ForwardCurrentMessageTo(string destination)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            busImpl.ForwardCurrentMessageTo(destination);
        }

        /// <summary>
        /// <see cref="IBus.DoNotContinueDispatchingCurrentMessageToHandlers"/>
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
           busImpl.DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        /// <summary>
        /// <see cref="IBus.CurrentMessageContext"/>
        /// </summary>
        public IMessageContext CurrentMessageContext { get { return busImpl.CurrentMessageContext; } }
    }
}
