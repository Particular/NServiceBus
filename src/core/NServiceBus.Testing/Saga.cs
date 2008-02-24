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

        private Saga(MockRepository mocks, IBus b)
        {
            m = mocks;
            bus = b;
        }

        public static Saga Test<T>(out T saga) where T : ISagaEntity, new()
        {
            saga = new T();
            saga.Id = Guid.NewGuid();

            MockRepository mocks = new MockRepository();
            IBus bus = mocks.DynamicMock<IBus>();

            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (typeof(IBus).IsAssignableFrom(field.FieldType))
                    field.SetValue(saga, bus);

            return new Saga(mocks, bus);
        }

        public Saga WhenReceivesMessageFrom(string client)
        {
            this.clientAddress = client;
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
            int returnCode = -1;

            Expect.Call(delegate { bus.Return(returnCode); })
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToReply(BusSendDelegate callback)
        {
            IMessage[] messages = null;

            Expect.Call(delegate { bus.Reply(messages); })
                .IgnoreArguments().Return(null)
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
