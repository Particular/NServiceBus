 ﻿namespace NServiceBus.Unicast.Tests
  {
      using System;
      using System.Collections.Generic;
      using System.Diagnostics;
      using System.Linq;
      using NServiceBus.Saga;
      using NServiceBus.Unicast.Behaviors;
      using NUnit.Framework;

      [TestFixture]
      [Explicit("Performance Tests")]
      public class HandlerInvocationCachePerformanceTests
      {
          [Test]
          public void RunNew()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubMessageHandlerOldStyle));
              cache.RegisterHandler(typeof(StubTimeoutHandlerOldStyle));
              cache.RegisterHandler(typeof(StubMessageHandlerNewStyle));
              cache.RegisterHandler(typeof(StubTimeoutHandlerNewStyle));
              cache.RegisterHandler(typeof(StubSubscribe));

              var handlerQueue = new Queue<object>();
              handlerQueue.Enqueue(new StubMessageHandlerOldStyle());
              handlerQueue.Enqueue(new StubMessageHandlerNewStyle());
              handlerQueue.Enqueue(new StubSubscribe());
              handlerQueue.Enqueue(new StubTimeoutHandlerOldStyle());
              handlerQueue.Enqueue(new StubTimeoutHandlerNewStyle());

              var handlerQueueCopy = new Queue<object>(handlerQueue);
              var copy = handlerQueueCopy;

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => copy.Dequeue(), h => new StubMessage());
              handlers = cache.GetHandlersFor(typeof(StubTimeoutState));
              handlers.InvokeAll(h => copy.Dequeue(), h => new StubTimeoutState());

              var startNew = Stopwatch.StartNew();
              for (var i = 0; i < 100000; i++)
              {
                  handlerQueueCopy = new Queue<object>(handlerQueue);
                  var queueCopy = handlerQueueCopy;
                  handlers = cache.GetHandlersFor(typeof(StubMessage));
                  handlers.InvokeAll(h => queueCopy.Dequeue(), h => new StubMessage());
                  handlers = cache.GetHandlersFor(typeof(StubTimeoutState));
                  handlers.InvokeAll(h => queueCopy.Dequeue(), h => new StubTimeoutState());
              }
              startNew.Stop();
              Trace.WriteLine(startNew.ElapsedMilliseconds);
          }

          class StubMessageHandlerOldStyle : IHandleMessages<StubMessage>
          {

              public void Handle(StubMessage message)
              {
              }
          }

          class StubMessageHandlerNewStyle : IHandle<StubMessage>
          {

              public void Handle(StubMessage message, IHandleContext context)
              {
              }
          }

          class StubSubscribe : ISubscribe<StubMessage>
          {

              public void Handle(StubMessage message, ISubscribeContext context)
              {
              }
          }

          class StubMessage : IMessage
          {
          }

          class StubTimeoutHandlerOldStyle : IHandleTimeouts<StubTimeoutState>
          {
              public void Timeout(StubTimeoutState state)
              {
              }
          }

          class StubTimeoutHandlerNewStyle : IHandleTimeout<StubTimeoutState>
          {
              public void Timeout(StubTimeoutState state, ITimeoutContext context)
              {
              }
          }

          class StubTimeoutState : IMessage
          {
          }
      }

      [TestFixture]
      public class When_invoking_a_cached_handler
      {
          [Test]
          public void Should_invoke_handle_method()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerOldStyle));
              cache.RegisterHandler(typeof(StubHandlerNewStyle));
              cache.RegisterHandler(typeof(StubSubscribe));

              var oldStyle = new StubHandlerOldStyle();
              var newStyle = new StubHandlerNewStyle();
              var newStyleSubscribe = new StubSubscribe();
              var queue = new Queue<object>();
              queue.Enqueue(oldStyle);
              queue.Enqueue(newStyle);
              queue.Enqueue(newStyleSubscribe);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => queue.Dequeue(), h => new StubMessage());

              Assert.IsTrue(oldStyle.HandleCalled);
              Assert.IsTrue(newStyle.HandleCalled);
              Assert.IsTrue(newStyleSubscribe.HandleCalled);
          }

          [Test]
          public void Should_have_passed_through_correct_message()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerOldStyle));
              cache.RegisterHandler(typeof(StubHandlerNewStyle));
              cache.RegisterHandler(typeof(StubSubscribe));

              var stubMessage = new StubMessage();

              var oldStyle = new StubHandlerOldStyle();
              var newStyle = new StubHandlerNewStyle();
              var newStyleSubscribe = new StubSubscribe();
              var queue = new Queue<object>();
              queue.Enqueue(oldStyle);
              queue.Enqueue(newStyle);
              queue.Enqueue(newStyleSubscribe);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => queue.Dequeue(), h => stubMessage);

              Assert.AreEqual(stubMessage, oldStyle.HandledMessage);
              Assert.AreEqual(stubMessage, newStyle.HandledMessage);
              Assert.AreEqual(stubMessage, newStyleSubscribe.HandledMessage);
          }

          [Test]
          public void Should_have_passed_through_context()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerNewStyle));
              cache.RegisterHandler(typeof(StubSubscribe));

              var handleContext = new StubHandleContext();
              var subscribeContext = new StubSubscribeContext();
              var contextQueue = new Queue<object>();
              contextQueue.Enqueue(handleContext);
              contextQueue.Enqueue(subscribeContext);

              var newStyle = new StubHandlerNewStyle();
              var newStyleSubscribe = new StubSubscribe();
              var handlerQueue = new Queue<object>();
              handlerQueue.Enqueue(newStyle);
              handlerQueue.Enqueue(newStyleSubscribe);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => handlerQueue.Dequeue(), h => new StubMessage(), h => contextQueue.Dequeue());

              Assert.AreEqual(handleContext, newStyle.Context);
              Assert.AreEqual(subscribeContext, newStyleSubscribe.Context);
          }

          class StubHandlerOldStyle : IHandleMessages<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;

              public void Handle(StubMessage message)
              {
                  HandleCalled = true;
                  HandledMessage = message;
              }
          }

          class StubHandlerNewStyle : IHandle<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;
              public IHandleContext Context;

              public void Handle(StubMessage message, IHandleContext context)
              {
                  HandleCalled = true;
                  HandledMessage = message;
                  Context = context;
              }
          }

          class StubSubscribe : ISubscribe<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;
              public ISubscribeContext Context;

              public void Handle(StubMessage message, ISubscribeContext context)
              {
                  HandleCalled = true;
                  HandledMessage = message;
                  Context = context;
              }
          }

          class StubMessage : IMessage
          {
          }
      }

      [TestFixture]
      public class When_invoking_a_cached_timeout_handler
      {
          [Test]
          public void Should_invoke_handler_for_each_handle_interface()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerOldStyle));
              cache.RegisterHandler(typeof(StubHandlerNewStyle));

              var oldStyle = new StubHandlerOldStyle();
              var newStyle = new StubHandlerNewStyle();
              var queue = new Queue<object>();
              queue.Enqueue(oldStyle);
              queue.Enqueue(newStyle);

              var handlers = cache.GetHandlersFor(typeof(StubTimeoutState));
              handlers.InvokeAll(h => queue.Dequeue(), h => new StubTimeoutState());

              Assert.IsTrue(oldStyle.TimeoutCalled);
              Assert.IsTrue(newStyle.TimeoutCalled);
          }

          [Test]
          public void Should_have_passed_through_correct_state()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerOldStyle));
              cache.RegisterHandler(typeof(StubHandlerNewStyle));

              var stubState = new StubTimeoutState();
              var oldStyle = new StubHandlerOldStyle();
              var newStyle = new StubHandlerNewStyle();
              var queue = new Queue<object>();
              queue.Enqueue(oldStyle);
              queue.Enqueue(newStyle);

              var handlers = cache.GetHandlersFor(typeof(StubTimeoutState));
              handlers.InvokeAll(h => queue.Dequeue(), h => stubState);

              Assert.AreEqual(stubState, oldStyle.HandledState);
              Assert.AreEqual(stubState, newStyle.HandledState);
          }

          [Test]
          public void Should_have_passed_through_context()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandlerNewStyle));

              var timeoutContext = new StubTimeoutContext();
              var newStyle = new StubHandlerNewStyle();

              var handlers = cache.GetHandlersFor(typeof(StubTimeoutState));
              handlers.InvokeAll(h => newStyle, h => new StubTimeoutState(), h => timeoutContext);

              Assert.AreEqual(timeoutContext, newStyle.Context);
          }

          class StubHandlerOldStyle : IHandleTimeouts<StubTimeoutState>
          {
              public bool TimeoutCalled;
              public StubTimeoutState HandledState;

              public void Timeout(StubTimeoutState state)
              {
                  TimeoutCalled = true;
                  HandledState = state;
              }
          }

          class StubHandlerNewStyle : IHandleTimeout<StubTimeoutState>
          {
              public bool TimeoutCalled;
              public StubTimeoutState HandledState;
              public ITimeoutContext Context;

              public void Timeout(StubTimeoutState state, ITimeoutContext context)
              {
                  TimeoutCalled = true;
                  HandledState = state;
                  Context = context;
              }
          }

          class StubTimeoutState : IMessage
          {
          }
      }

      [TestFixture]
      public class When_invoking_a_handler_which_implements_multiple_interfaces_including_message_hierarchies
      {
          [Test]
          public void Should_invoke_handler_for_each_handle_interface()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(WorstCaseHandlerNewStyle));

              var handlers = cache.GetHandlersFor(typeof(StubMessage)).ToList();

              var newStyle = new WorstCaseHandlerNewStyle();
              handlers.InvokeAll(h => newStyle, h => new StubMessage());

              Assert.AreEqual(9, handlers.Count());
              Assert.AreEqual(1, newStyle.OldStyleHandleCalled);
              Assert.AreEqual(1, newStyle.OldStyleHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.OldStyleHandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.NewStyleHandleCalled);
              Assert.AreEqual(1, newStyle.NewStyleHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.NewStyleHandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.SubscribeCalled);
              Assert.AreEqual(1, newStyle.SubscribeOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.SubscribeOverloadIMessageCalled);
          }

          class WorstCaseHandlerNewStyle : IHandle<StubMessage>, IHandle<IStubMessage>, IHandle<IMessage>, ISubscribe<StubMessage>, ISubscribe<IStubMessage>, ISubscribe<IMessage>, IHandleMessages<StubMessage>, IHandleMessages<IStubMessage>, IHandleMessages<IMessage>
          {
              public int OldStyleHandleCalled;
              public int OldStyleHandleOverloadIStubMessageCalled;
              public int OldStyleHandleOverloadIMessageCalled;
              public int NewStyleHandleCalled;
              public int SubscribeCalled;
              public int NewStyleHandleOverloadIStubMessageCalled;
              public int NewStyleHandleOverloadIMessageCalled;
              public int SubscribeOverloadIStubMessageCalled;
              public int SubscribeOverloadIMessageCalled;

              public void Handle(StubMessage state, IHandleContext context)
              {
                  NewStyleHandleCalled += 1;
              }

              public void Handle(StubMessage message, ISubscribeContext context)
              {
                  SubscribeCalled += 1;
              }

              public void Handle(StubMessage message)
              {
                  OldStyleHandleCalled += 1;
              }

              public void Handle(IStubMessage message)
              {
                  OldStyleHandleOverloadIStubMessageCalled += 1;
              }

              public void Handle(IMessage message)
              {
                  OldStyleHandleOverloadIMessageCalled += 1;
              }

              public void Handle(IStubMessage message, IHandleContext context)
              {
                  NewStyleHandleOverloadIStubMessageCalled += 1;
              }

              public void Handle(IMessage message, IHandleContext context)
              {
                  NewStyleHandleOverloadIMessageCalled += 1;
              }

              public void Handle(IStubMessage message, ISubscribeContext context)
              {
                  SubscribeOverloadIStubMessageCalled += 1;
              }

              public void Handle(IMessage message, ISubscribeContext context)
              {
                  SubscribeOverloadIMessageCalled += 1;
              }
          }

          interface IStubMessage : IMessage { }

          class StubMessage : IStubMessage
          {
          }
      }

      static class MessageHandlerExtensions
      {
          public static void InvokeAll(this IEnumerable<MessageHandler> handlers, Func<MessageHandler, object> instanceAssignment, Func<MessageHandler, object> messageFactory, Func<MessageHandler, object> contextFactory = null)
          {
              contextFactory = contextFactory ?? DefaultContextFactory;
              foreach (var handler in handlers)
              {
                  handler.Instance = instanceAssignment(handler);
                  handler.Invoke(messageFactory(handler), contextFactory(handler));
              }
          }

          static object DefaultContextFactory(MessageHandler handler)
          {
              object context;
              switch (handler.HandlerKind)
              {
                  case HandlerKind.Message:
                      context = new StubHandleContext();
                      break;
                  case HandlerKind.Event:
                      context = new StubSubscribeContext();
                      break;
                  case HandlerKind.Timeout:
                      context = new StubTimeoutContext();
                      break;
                  default:
                      throw new Exception();
              }
              return context;
          }
      }
      class StubTimeoutContext : ITimeoutContext { }
      class StubSubscribeContext : ISubscribeContext { }
      class StubHandleContext : IHandleContext { }
  }

