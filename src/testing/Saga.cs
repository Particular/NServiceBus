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
        
        private ISagaEntity sagaData;

        private Saga(MockRepository mocks, IBus b, ISagaEntity sagaData, IMessageCreator messageCreator)
        {
            m = mocks;
            bus = b;
            this.sagaData = sagaData;
            this.messageCreator = messageCreator;
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

            var result = new Saga(mocks, bus, sagaData, mapper);

            foreach (Type t in typesToMap)
                typeof(Saga).GetMethod("SetupResultForBusCreateInstance").MakeGenericMethod(t).Invoke(result, null);

            return result;
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
                SetupResult.For(bus.SourceOfMessageBeingHandled).Return(this.clientAddress);

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

            Expect.Call(delegate { bus.Publish(messages); }).IgnoreArguments().Callback(callback);
        }

        public void SetupResultForBusCreateInstance<T>() where T : IMessage
        {
            T result = this.messageCreator.CreateInstance<T>();

            CreateInstanceDelegate<T> callback = new CreateInstanceDelegate<T>(
                delegate(Action<T> action)
                {
                    action(result);
                    return true;
                }
            );

            Delegate d = new HandleMessageDelegate(
                delegate
                {
                    Action<T> act = null;
                    SetupResult.For(bus.CreateInstance<T>(act)).IgnoreArguments().Return(result).Callback(callback);
                }
            );

            this.delegates.Add(d);


        }

    }

}
