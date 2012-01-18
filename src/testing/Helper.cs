using System;
using System.Collections.Generic;
using System.Reflection;
using Rhino.Mocks;
using Rhino.Mocks.Exceptions;

namespace NServiceBus.Testing
{
    internal class Helper
    {
        private readonly IBus bus;
        private readonly MockRepository m;
        private readonly IMessageCreator messageCreator;
        private readonly List<Delegate> delegates = new List<Delegate>();
        private readonly IEnumerable<Type> messageTypes = new List<Type>();

        public Helper(MockRepository mocks, IBus b, IMessageCreator messageCreator, IEnumerable<Type> types)
        {
            m = mocks;
            bus = b;
            this.messageCreator = messageCreator;
            messageTypes = types;
        }

        /// <summary>
        /// Check that the object sends a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectSend<TMessage>(SendPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend<TMessage>(
                          delegate(object[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object replies with the given message type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectReply<TMessage>(SendPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToReply(
                          delegate(object[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object sends the given message type to its local queue
        /// and that the message complies with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectSendLocal<TMessage>(SendPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSendLocal<TMessage>(
                          delegate(object[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object uses the bus to return the appropriate error code.
        /// </summary>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectReturn(ReturnPredicate check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToReturn(check)
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object sends the given message type to the appropriate destination.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectSendToDestination<TMessage>(SendToDestinationPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend<TMessage>(
                          delegate(string destination, object[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(destination, msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object replies to the originator with the given message type.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectReplyToOrginator<TMessage>(SendPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToSend<TMessage>(
                          delegate(string destination, string correlationId, object[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Check that the object publishes a message of the given type complying with the given predicate.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="check"></param>
        /// <returns></returns>
        public void ExpectPublish<TMessage>(PublishPredicate<TMessage> check)
        {
            Delegate d = new HandleMessageDelegate(
                () => ExpectCallToPublish(
                          delegate(TMessage[] msgs)
                              {
                                  foreach (TMessage msg in msgs)
                                      if (!check(msg))
                                          return false;

                                  return true;
                              }
                          )
                );

            delegates.Add(d);
        }

		/// <summary>
		/// Check that the object does not publish any messages of the given type complying with the given predicate.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="check"></param>
		/// <returns></returns>
		public void ExpectNotPublish<TMessage>(PublishPredicate<TMessage> check)
		{
			Delegate d = new HandleMessageDelegate(
				() => DoNotExpectCallToPublish<TMessage>(
						  delegate(TMessage[] msgs)
						  {
							  foreach (TMessage msg in msgs)
								  if (!check(msg))
									  return false;

							  return true;
						  }
						  )
				);

			delegates.Add(d);
		}
		
		/// <summary>
        /// Check that the object tells the bus to not dispatch the current message to any other handlers.
        /// </summary>
        /// <returns></returns>
        public void ExpectDoNotContinueDispatchingCurrentMessageToHandlers()
        {
            Delegate d = new HandleMessageDelegate(
                this.ExpectCallToDoNotContinueDispatchingCurrentMessageToHandlers
                );

            delegates.Add(d);
        }

        public void ExpectForwardCurrentMessageTo(string destination)
        {
            string dest = string.Empty;

            Delegate del = new HandleMessageDelegate(
                () => Expect.Call(() => bus.ForwardCurrentMessageTo(dest))
                        .IgnoreArguments()
                        .Callback((string d) => d == destination)
                );

            delegates.Add(del);
        }

        /// <summary>
        /// Check that the object tells the bus to handle the current message later
        /// </summary>
        /// <returns></returns>
        public void ExpectHandleCurrentMessageLater()
        {
            Delegate d = new HandleMessageDelegate(
                this.ExpectCallToHandleCurrentMessageLater
                );

            delegates.Add(d);
        }

        /// <summary>
        /// Uses the given delegate to invoke the object, checking all the expectations previously set up,
        /// and then clearing them for continued testing.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="doSomething"></param>
        public void Go(MessageContext context, Action doSomething)
        {
            using (m.Record())
            {
                foreach (var t in messageTypes)
                    GetType().GetMethod("PrepareBusGenericMethods", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(t).Invoke(this, null);

                SetupResult.For(bus.CurrentMessageContext).Return(context);

                foreach (var d in delegates)
                    d.DynamicInvoke();
            }

            using (m.Playback())
                doSomething();

            m.BackToRecordAll();

            delegates.Clear();
        }

        public void ExpectDefer<T>(Func<TimeSpan,object[],bool> func)
        {
            Expect.Call(() => bus.Defer(TimeSpan.FromDays(1),null))
                .IgnoreArguments().Return(null)
                .Callback(func);
        }


        private void ExpectCallToReturn(ReturnPredicate callback)
        {
            Expect.Call(() => bus.Return(-1))
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToReply(BusSendDelegate callback)
        {
            object[] messages = null;

            Expect.Call(() => bus.Reply(messages))
                .IgnoreArguments()
                .Callback(callback);
        }

        private void ExpectCallToSendLocal<T>(BusSendDelegate callback)
        {
            Expect.Call(() => bus.SendLocal(Arg<T>.Is.Anything))
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend<T>(BusSendDelegate callback)
        {
            Expect.Call(() => bus.Send(Arg<T>.Is.Anything))
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend<T>(BusSendWithDestinationDelegate callback)
        {
            Expect.Call(() => bus.Send(Arg<string>.Is.NotNull, Arg<T>.Is.Anything))
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToSend<T>(BusSendWithDestinationAndCorrelationIdDelegate callback)
        {
            Expect.Call(() => bus.Send(Arg<string>.Is.NotNull, Arg<string>.Is.NotNull, Arg<T>.Is.Anything))
                .IgnoreArguments().Return(null)
                .Callback(callback);
        }

        private void ExpectCallToPublish<T>(BusPublishDelegate<T> callback)
        {
            T[] messages = null;

            Expect.Call(() => bus.Publish(messages))
                .IgnoreArguments()
                .Callback(callback);
        }

		private void DoNotExpectCallToPublish<T>(BusPublishDelegate<T> callback)
		{
            bus.Stub(b => bus.Publish(Arg<T>.Is.Anything))
				.IgnoreArguments()
				.Callback(callback)
				.Throw(
					new ExpectationViolationException(string.Format("Did not expect a call to Publish<{0}> matching predicate {1}",
					                                                typeof (T).FullName, callback)));
		}

        private void ExpectCallToDoNotContinueDispatchingCurrentMessageToHandlers()
        {
            Expect.Call(() => bus.DoNotContinueDispatchingCurrentMessageToHandlers())
                .IgnoreArguments();
        }

        private void ExpectCallToHandleCurrentMessageLater()
        {
            Expect.Call(() => bus.HandleCurrentMessageLater())
                .IgnoreArguments();
        }

        /// <summary>
        /// Invoked via reflection - do not remove.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        private void PrepareBusGenericMethods<TMessage>()
        {
            var d = GetDelegateForBusGenericMethods<TMessage>();

            delegates.Add(d);
        }

        private Delegate GetDelegateForBusGenericMethods<TMessage>()
        {
            return new HandleMessageDelegate(
                delegate
                {
                    Action<TMessage> act = null;
                    string destination = null;

                    bus.CreateInstance<TMessage>();
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        mi.ReturnValue = messageCreator.CreateInstance<TMessage>();
                    }
                    );

                    bus.CreateInstance(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        var action = mi.Arguments[0] as Action<TMessage>;
                        mi.ReturnValue = messageCreator.CreateInstance(action);
                    }
                    );

                    bus.Reply(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        var action = mi.Arguments[0] as Action<TMessage>;
                        bus.Reply(messageCreator.CreateInstance(action));
                    }
                    );

                    bus.Send(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        var action = mi.Arguments[0] as Action<TMessage>;
                        bus.Send(messageCreator.CreateInstance(action));
                    }
                    );

                    bus.Send(destination, act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        var dest = mi.Arguments[0] as string;
                        var action = mi.Arguments[1] as Action<TMessage>;
                        bus.Send(dest, messageCreator.CreateInstance(action));
                    }
                    );

                    bus.SendLocal(act);
                    LastCall.Repeat.Any().IgnoreArguments().Return(null).WhenCalled(mi =>
                    {
                        var action = mi.Arguments[0] as Action<TMessage>;
                        bus.SendLocal(messageCreator.CreateInstance(action));
                    }
                    );

                    bus.Publish(act);
                    LastCall.Repeat.Any().IgnoreArguments().WhenCalled(mi =>
                    {
                        var action = mi.Arguments[0] as Action<TMessage>;
                        bus.Publish(messageCreator.CreateInstance(action));
                    }
                    );

                }
            );
        }
    }
}
