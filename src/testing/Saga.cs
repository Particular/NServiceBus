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
        private string clientAddress;
        private readonly List<Delegate> delegates = new List<Delegate>();
        
        private ISagaEntity sagaData;

        private Saga(MockRepository mocks, IBus b, ISagaEntity sagaData)
        {
            m = mocks;
            bus = b;
            this.sagaData = sagaData;
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

            return new Saga(mocks, bus, sagaData);
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

    }

}
