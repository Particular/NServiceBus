using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Saga;

namespace NServiceBus.Testing
{
    public class StubBus : IBus
    {
        private readonly IMessageCreator messageCreator;
        private readonly IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();
        private IMessageContext messageContext;
        private readonly List<ActualInvocation> actualInvocations = new List<ActualInvocation>();
        private readonly TimeoutManager timeoutManager = new TimeoutManager();

        public void ValidateAndReset(IEnumerable<IExpectedInvocation> expectedInvocations)
        {
            expectedInvocations.ToList().ForEach(e => e.Validate(actualInvocations.ToArray()));

            actualInvocations.Clear();
        }

        public object PopTimeout()
        {
            return timeoutManager.Pop();
        }

        public StubBus(IMessageCreator creator)
        {
            messageCreator = creator;
        }

        public void Publish<T>(params T[] messages)
        {
            if (messages.Length == 0)
            {
                ProcessInvocation(typeof(PublishInvocation<>), CreateInstance<T>());
                return;
            }

            foreach (var message in messages)
            {
                ProcessInvocation(typeof(PublishInvocation<>), message);
            }
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            Publish(messageCreator.CreateInstance(messageConstructor));
        }

        public void Subscribe(Type messageType)
        {
            throw new NotSupportedException();
        }

        public void Subscribe<T>()
        {
            throw new NotSupportedException();
        }

        public void Subscribe(Type messageType, Predicate<object> condition)
        {
            throw new NotSupportedException();
        }

        public void Subscribe<T>(Predicate<T> condition)
        {
            throw new NotSupportedException();
        }

        public void Unsubscribe(Type messageType)
        {
            throw new NotSupportedException();
        }

        public void Unsubscribe<T>()
        {
            throw new NotSupportedException();
        }

        public ICallback SendLocal(params object[] messages)
        {
            return ProcessInvocation(typeof(SendLocalInvocation<>), messages);
        }

        public ICallback SendLocal<T>(Action<T> messageConstructor)
        {
            return SendLocal(messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback Send(params object[] messages)
        {
            return Send(Address.Undefined, messages);
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return Send(string.Empty, messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback Send(string destination, params object[] messages)
        {
            if (destination == string.Empty)
                return Send(Address.Undefined, messages);
            
            return Send(Address.Parse(destination), messages);
        }

        public ICallback Send(Address address, params object[] messages)
        {
            return Send(address, String.Empty, messages);
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return Send(destination, messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback Send<T>(Address address, Action<T> messageConstructor)
        {
            return Send(address, messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback Send(string destination, string correlationId, params object[] messages)
        {
            if (destination == string.Empty)
                return Send(Address.Undefined, correlationId, messages);

            return Send(Address.Parse(destination), correlationId, messages);
        }

        public ICallback Send(Address address, string correlationId, params object[] messages)
        {
            if (address != Address.Undefined && correlationId != string.Empty)
            {
                var d = new Dictionary<string, object> {{"Address", address}, {"CorrelationId", correlationId}};
                return ProcessInvocation(typeof (ReplyToOriginatorInvocation<>), d, messages);
            }

            if (address != Address.Undefined && correlationId == string.Empty)
                return ProcessInvocation(typeof(SendToDestinationInvocation<>), new Dictionary<string, object> { { "Address", address } }, messages);

            return ProcessInvocation(typeof(SendInvocation<>), messages);
        }

        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return Send(destination, correlationId, messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback Send<T>(Address address, string correlationId, Action<T> messageConstructor)
        {
            return Send(address, correlationId, messageCreator.CreateInstance(messageConstructor));
        }

        public ICallback SendToSites(IEnumerable<string> siteKeys, params object[] messages)
        {
            return ProcessInvocation(typeof(SendToSitesInvocation<>), siteKeys, messages);
        }

        public ICallback Defer(TimeSpan delay, params object[] messages)
        {
            return ProcessDefer<TimeSpan>(delay, messages);
        }

        public ICallback Defer(DateTime processAt, params object[] messages)
        {
            return ProcessDefer<DateTime>(processAt, messages);
        }

        public void Reply(params object[] messages)
        {
            ProcessInvocation(typeof(ReplyInvocation<>), messages);
        }

        public void Reply<T>(Action<T> messageConstructor)
        {
            Reply(messageCreator.CreateInstance(messageConstructor));
        }

        public void Return<T>(T errorEnum)
        {
            actualInvocations.Add(new ReturnInvocation<T> { Value = errorEnum});
        }

        public void HandleCurrentMessageLater()
        {
            actualInvocations.Add(new HandleCurrentMessageLaterInvocation<object>());
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            actualInvocations.Add(new ForwardCurrentMessageToInvocation { Value = destination });
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            actualInvocations.Add(new DoNotContinueDispatchingCurrentMessageToHandlersInvocation<object>());
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get { return outgoingHeaders; }
        }

        public IMessageContext CurrentMessageContext
        {
            get { return messageContext; }
            set { messageContext = value; }
        }

        public IInMemoryOperations InMemory
        {
            get { throw new NotImplementedException(); }
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public IBus Start(Action startupAction)
        {
            throw new NotImplementedException();
        }

        public IBus Start()
        {
            throw new NotImplementedException();
        }

        public event EventHandler Started;

        public T CreateInstance<T>()
        {
            return messageCreator.CreateInstance<T>();
        }

        public T CreateInstance<T>(Action<T> action)
        {
            return messageCreator.CreateInstance(action);
        }

        public object CreateInstance(Type messageType)
        {
            return messageCreator.CreateInstance(messageType);
        }

        private ICallback ProcessInvocation(Type genericType, params object[] messages)
        {
            return ProcessInvocation(genericType, new Dictionary<string, object>(), messages);
        }

        private ICallback ProcessInvocation(Type genericType, Dictionary<string, object> others, object[] messages)
        {
            foreach (var message in messages)
            {
                var messageType = GetMessageType(message);
                var invocationType = genericType.MakeGenericType(messageType);
                ProcessInvocationWithBuiltType(invocationType, others, new[] {message});
            }

            return null;
        }

        private ICallback ProcessInvocation<K>(Type dualGenericType, Dictionary<string, object> others, params object[] messages)
        {
            foreach (var message in messages)
            {
                var invocationType = dualGenericType.MakeGenericType(GetMessageType(message), typeof (K));
                ProcessInvocationWithBuiltType(invocationType, others, new[] {message});
            }

            return null;
        }

        private ICallback ProcessInvocationWithBuiltType(Type builtType, Dictionary<string, object> others, object[] messages)
        {
            if (messages == null)
                throw new NullReferenceException("messages is null.");

            if (messages.Length == 0)
                throw new InvalidOperationException("messages should not be empty.");

            var invocation = Activator.CreateInstance(builtType) as ActualInvocation;

            builtType.GetProperty("Messages").SetValue(invocation, messages, null);

            foreach (var kv in others)
                builtType.GetProperty(kv.Key).SetValue(invocation, kv.Value, null);

            actualInvocations.Add(invocation);

            return null;
        }

        private Type GetMessageType(object message)
        {
            if (message.GetType().FullName.EndsWith("__impl"))
            {
                var name = message.GetType().FullName.Replace("__impl", "").Replace("\\","");
                foreach (var i in message.GetType().GetInterfaces())
                    if (i.FullName == name)
                        return i;
            }

            return message.GetType();
        }

        private ICallback ProcessDefer<T>(object delayOrProcessAt, params object[] messages)
        {
            timeoutManager.Push(delayOrProcessAt, messages[0]);
            return ProcessInvocation<T>(typeof(DeferMessageInvocation<,>), new Dictionary<string, object> { { "Value", delayOrProcessAt } }, messages);
        }
    }
}
