using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Saga;

namespace NServiceBus.Testing
{
    public class StubBus : IBus
    {
        private IMessageCreator messageCreator;
        private IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();
        private IMessageContext messageContext;

        private readonly List<ActualInvocation> ActualInvocations = new List<ActualInvocation>();

        public void ValidateAndReset(IEnumerable<IExpectedInvocation> expectedInvocations)
        {
            expectedInvocations.ToList().ForEach(e => e.Validate(ActualInvocations.ToArray()));

            ActualInvocations.Clear();
        }

        public StubBus(IMessageCreator creator)
        {
            messageCreator = creator;
        }

        public void Publish<T>(params T[] messages)
        {
            if (messages.Length > 1)
                throw new NotSupportedException("Testing doesn't support publishing multiple messages.");
            if (messages.Length == 0)
            {
                ProcessInvocation(typeof(PublishInvocation<>), CreateInstance<T>());
                return;
            }

            ProcessInvocation(typeof(PublishInvocation<>), messages[0]);
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
            if (messages.Length == 1)
                if (messages[0] is TimeoutMessage)
                    return ProcessInvocation<TimeSpan>(typeof(DeferMessageInvocation<,>), new Dictionary<string, object> { { "Value", delay } }, (messages[0] as TimeoutMessage).State);

            return ProcessInvocation<TimeSpan>(typeof(DeferMessageInvocation<,>), new Dictionary<string, object> { { "Value", delay } }, messages);
        }

        public ICallback Defer(DateTime processAt, params object[] messages)
        {
            if (messages.Length == 1)
                if (messages[0] is TimeoutMessage)
                    return ProcessInvocation<DateTime>(typeof(DeferMessageInvocation<,>), new Dictionary<string, object> { { "Value", processAt } }, (messages[0] as TimeoutMessage).State);

            return ProcessInvocation<DateTime>(typeof(DeferMessageInvocation<,>), new Dictionary<string, object> { { "Value", processAt } }, messages);
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
            ActualInvocations.Add(new ReturnInvocation<T> { Value = errorEnum});
        }

        public void HandleCurrentMessageLater()
        {
            ActualInvocations.Add(new HandleCurrentMessageLaterInvocation<object>());
        }

        public void ForwardCurrentMessageTo(string destination)
        {
            ActualInvocations.Add(new ForwardCurrentMessageToInvocation { Value = destination });
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            ActualInvocations.Add(new DoNotContinueDispatchingCurrentMessageToHandlersInvocation<object>());
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
            var invocationType = genericType.MakeGenericType(GetMessageType(messages[0]));
            return ProcessInvocationWithBuiltType(invocationType, others, messages);
        }

        private ICallback ProcessInvocation<K>(Type dualGenericType, Dictionary<string, object> others, params object[] messages)
        {
            var invocationType = dualGenericType.MakeGenericType(GetMessageType(messages[0]), typeof(K));
            return ProcessInvocationWithBuiltType(invocationType, others, messages);
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

            ActualInvocations.Add(invocation);

            return null;
        }

        private Type GetMessageType(object message)
        {
            if (message.GetType().Name.EndsWith("__impl"))
                return message.GetType().GetInterface(message.GetType().Name.Replace("__impl", ""));

            return message.GetType();
        }
    }
}
