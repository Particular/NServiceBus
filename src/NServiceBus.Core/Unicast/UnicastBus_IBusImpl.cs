using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast
{
    public partial class UnicastBus
    {
        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}()"/>
        /// </summary>
        /// <param name="message"></param>
        /// <typeparam name="T"></typeparam>
        public void Publish<T>(T message)
        {
            busImpl.Publish(message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Publish{T}(T)"/>
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
            busImpl.Publish(messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(object)"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Send(object message)
        {
            return busImpl.Send(message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return busImpl.Send(messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string, object)"/>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Send(string destination, object message)
        {
            return busImpl.Send(destination, message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(Address, object)"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Send(Address address, object message)
        {
            return busImpl.Send(address, message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(string, Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return busImpl.Send(destination, messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Address, Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return busImpl.Send(address, messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(string, string, object)"/>
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Send(string destination, string correlationId, object message)
        {
            return busImpl.Send(destination, correlationId, message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send(Address, string, object)"/>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public ICallback Send(Address address, string correlationId, object message)
        {
            return busImpl.Send(address, correlationId, message);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(string, string, Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return busImpl.Send(destination, correlationId, messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.Send{T}(Address, string, Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <param name="correlationId"></param>
        /// <param name="messageConstructor"></param>
        /// <returns></returns>
        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            return busImpl.Send(address, correlationId, messageConstructor);
        }

        /// <summary>
        /// <see cref="ISendOnlyBus.OutgoingHeaders"/>
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders { get { return busImpl.OutgoingHeaders; } }

        /// <summary>
        /// <see cref="IBus.Subscribe"/>
        /// </summary>
        /// <param name="messageType"></param>
        public void Subscribe(Type messageType)
        {
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
            return busImpl.Defer(processAt, message);
        }

        /// <summary>
        /// <see cref="IBus.Reply"/>
        /// </summary>
        /// <param name="message"></param>
        public void Reply(object message)
        {
            busImpl.Reply(message);
        }

        /// <summary>
        /// <see cref="IBus.Reply{T}(Action{T})"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        public void Reply<T>(Action<T> messageConstructor)
        {
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

        /// <summary>
        /// <see cref="IBus.InMemory"/>
        /// </summary>
        public IInMemoryOperations InMemory { get { return busImpl.InMemory; } }
    }
}
