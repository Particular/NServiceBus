using System;
using System.Collections.Generic;
using System.Reflection;
using NServiceBus.Saga;
using Rhino.Mocks;

namespace NServiceBus.Testing
{
    public class Saga
    {
        private readonly IBus bus;
        private readonly MockRepository m;
        private readonly IMessageCreator messageCreator;
        private string clientAddress;
        private readonly List<Delegate> delegates = new List<Delegate>();
        private readonly List<Type> messageTypes = new List<Type>();
        private IDictionary<string, string> incomingHeaders = new Dictionary<string, string>();
        private IDictionary<string, string> outgoingHeaders = new Dictionary<string, string>();
        
        private ISagaEntity sagaData;

        private Saga(MockRepository mocks, IBus b, ISagaEntity sagaData, IMessageCreator messageCreator, List<Type> types)
        {
            m = mocks;
            bus = b;
            this.sagaData = sagaData;
            this.messageCreator = messageCreator;
            ExtensionMethods.MessageCreator = messageCreator;
            messageTypes = types;
        }

        public static Saga Test<T>(out T saga) where T : ISaga, new()
        {
            saga = (T)Activator.CreateInstance(typeof (T));

            PropertyInfo prop = typeof (T).GetProperty("Data");
            ISagaEntity sagaData = Activator.CreateInstance(prop.PropertyType) as ISagaEntity;

            saga.Entity = sagaData;

            saga.Entity.Id = Guid.NewGuid();

            MockRepository mocks = new MockRepository();
            IBus bus = mocks.DynamicMock<IBus>();

            saga.Bus = bus;

            var mapper = new NServiceBus.MessageInterfaces.MessageMapper.Reflection.MessageMapper();
            var typesToMap = new List<Type>();
            foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                try
                {
                    foreach (Type t in a.GetTypes())
                        if (typeof(IMessage).IsAssignableFrom(t))
                            if (!typesToMap.Contains(t))
                                typesToMap.Add(t);
                }
                catch
                {
                    //swallow exceptions on purpose
                }

            mapper.Initialize(typesToMap.ToArray());

            return new Saga(mocks, bus, sagaData, mapper, typesToMap);
        }

        public T CreateInstance<T>() where T : IMessage
        {
            return messageCreator.CreateInstance<T>();
        }

        public T CreateInstance<T>(Action<T> action) where T : IMessage
        {
            return messageCreator.CreateInstance<T>(action);
        }

        public Saga WhenReceivesMessageFrom(string client)
        {
            this.clientAddress = client;
            this.sagaData.Originator = client;

            return this;
        }

        public Saga SetIncomingHeaders(IDictionary<string, string> headers)
        {
            this.incomingHeaders = headers;

            return this;
        }

        public IDictionary<string, string> OutgoingHeaders
        {
            get { return outgoingHeaders; }
        }

        public Saga ExpectSend<T>(SendPredicate<T> check) where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToSend(
                        delegate(IMessage[] msgs)
                        {
                            foreach (T msg in msgs)
                                if (!check(msg))
                                    return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public Saga ExpectReply<T>(SendPredicate<T> check) where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToReply(
                        delegate(IMessage[] msgs)
                        {
                            foreach (T msg in msgs)
                                if (!check(msg))
                                    return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public Saga ExpectSendLocal<T>(SendPredicate<T> check) where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToSendLocal(
                        delegate(IMessage[] msgs)
                        {
                            foreach (T msg in msgs)
                                if (!check(msg))
                                    return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public Saga ExpectReturn(ReturnPredicate check)
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToReturn(
                        delegate(int returnCode)
                        {
                            if (!check(returnCode))
                                return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public Saga ExpectSendToDestination<T>(SendToDestinationPredicate<T> check) where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToSend(
                        delegate(string destination, IMessage[] msgs)
                        {
                            foreach (T msg in msgs)
                                if (!check(destination, msg))
                                    return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public Saga ExpectPublish<T>(PublishPredicate<T> check) where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    ExpectCallToPublish<T>(
                        delegate(T[] msgs)
                        {
                            foreach (T msg in msgs)
                                if (!check(msg))
                                    return false;

                            return true;
                        }
                        );
                }
            );

            this.delegates.Add(d);
            return this;
        }

        public void When(HandleMessageDelegate handle)
        {
            using (m.Record())
            {
                foreach (Type t in messageTypes)
                    typeof(Saga).GetMethod("PrepareBusGenericMethods").MakeGenericMethod(t).Invoke(this, null);

                SetupResult.For(bus.SourceOfMessageBeingHandled).Return(this.clientAddress);
                SetupResult.For(bus.IncomingHeaders).Return(this.incomingHeaders);
                SetupResult.For(bus.OutgoingHeaders).Return(this.outgoingHeaders);

                foreach (Delegate d in this.delegates)
                    d.DynamicInvoke();
            }

            using (m.Playback())
                handle();

            m.BackToRecordAll();

            this.delegates.Clear();
        }

        private void ExpectCallToReturn(ReturnPredicate callback)
        {
            Expect.Call(delegate { bus.Return(-1); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToReply(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.Reply(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToSendLocal(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.SendLocal(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToSend(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.Send(messages); })
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend(BusSendWithDestinationDelegate callback)
        {
            IMessage[] messages = null;
            string destination = null;

            Expect.Call(delegate { bus.Send(destination, messages); })
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToPublish<T>(BusPublishDelegate<T> callback) where T : IMessage
        {
            T[] messages = null;

            Expect.Call(delegate { bus.Publish(messages); })
                .IgnoreArguments()
                .Callback(callback);
        }

        public void PrepareBusGenericMethods<T>() where T : IMessage
        {
            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    Action<T> act = null;
                    string destination = null;

                    bus.CreateInstance<T>();
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        mi.ReturnValue = this.messageCreator.CreateInstance<T>();
                    }
                    );

                    bus.CreateInstance<T>(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        Action<T> action = mi.Arguments[0] as Action<T>;
                        mi.ReturnValue = this.messageCreator.CreateInstance<T>(action);
                    }
                    );

                    bus.Reply<T>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<T> action = mi.Arguments[0] as Action<T>;
                        bus.Reply(this.messageCreator.CreateInstance<T>(action));
                    }
                    );

                    bus.Send<T>(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        Action<T> action = mi.Arguments[0] as Action<T>;
                        bus.Send(this.messageCreator.CreateInstance<T>(action));
                    }
                    );
                    
                    bus.Send<T>(destination, act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        string dest = mi.Arguments[0] as string;
                        Action<T> action = mi.Arguments[1] as Action<T>;
                        bus.Send(dest, this.messageCreator.CreateInstance<T>(action));
                    }
                    );

                    bus.SendLocal<T>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<T> action = mi.Arguments[0] as Action<T>;
                        bus.SendLocal(this.messageCreator.CreateInstance<T>(action));
                    }
                    );

                    bus.Publish<T>(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        Action<T> action = mi.Arguments[0] as Action<T>;
                        bus.Publish(this.messageCreator.CreateInstance<T>(action));
                    }
                    );

                }
            );

            this.delegates.Add(d);
        }

    }

}
