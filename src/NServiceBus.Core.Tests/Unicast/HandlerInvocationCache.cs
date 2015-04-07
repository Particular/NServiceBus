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
              cache.RegisterHandler(typeof(StubMessageHandler));
              cache.RegisterHandler(typeof(StubTimeoutHandlerOldStyle));
              cache.RegisterHandler(typeof(StubCommandHandler));
              cache.RegisterHandler(typeof(StubTimeoutHandlerNewStyle));
              cache.RegisterHandler(typeof(StubEventHandler));
              cache.RegisterHandler(typeof(StubResponseHandler));

              var handlerQueue = new Queue<object>();
              handlerQueue.Enqueue(new StubMessageHandler());
              handlerQueue.Enqueue(new StubCommandHandler());
              handlerQueue.Enqueue(new StubEventHandler());
              handlerQueue.Enqueue(new StubResponseHandler());
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

          class StubMessageHandler : IHandleMessages<StubMessage>
          {

              public void Handle(StubMessage message)
              {
              }
          }

          class StubCommandHandler : IProcessCommands<StubMessage>
          {

              public void Handle(StubMessage message, ICommandContext context)
              {
              }
          }

          class StubEventHandler : IProcessEvents<StubMessage>
          {

              public void Handle(StubMessage message, IEventContext context)
              {
              }
          }

          class StubResponseHandler : IProcessResponses<StubMessage>
          {

              public void Handle(StubMessage message, IResponseContext context)
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

          class StubTimeoutHandlerNewStyle : IProcessTimeouts<StubTimeoutState>
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
              cache.RegisterHandler(typeof(StubHandler));
              cache.RegisterHandler(typeof(StubCommandHandler));
              cache.RegisterHandler(typeof(StubEventHandler));
              cache.RegisterHandler(typeof(StubResponseHandler));

              var handler = new StubHandler();
              var commandHandler = new StubCommandHandler();
              var eventHandler = new StubEventHandler();
              var responseHandler = new StubResponseHandler();
              var queue = new Queue<object>();
              queue.Enqueue(handler);
              queue.Enqueue(commandHandler);
              queue.Enqueue(eventHandler);
              queue.Enqueue(responseHandler);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => queue.Dequeue(), h => new StubMessage());

              Assert.IsTrue(handler.HandleCalled);
              Assert.IsTrue(commandHandler.HandleCalled);
              Assert.IsTrue(eventHandler.HandleCalled);
              Assert.IsTrue(responseHandler.HandleCalled);
          }

          [Test]
          public void Should_have_passed_through_correct_message()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubHandler));
              cache.RegisterHandler(typeof(StubCommandHandler));
              cache.RegisterHandler(typeof(StubEventHandler));
              cache.RegisterHandler(typeof(StubResponseHandler));

              var stubMessage = new StubMessage();

              var handler = new StubHandler();
              var commandHandler = new StubCommandHandler();
              var eventHandler = new StubEventHandler();
              var responseHandler = new StubResponseHandler();
              var queue = new Queue<object>();
              queue.Enqueue(handler);
              queue.Enqueue(commandHandler);
              queue.Enqueue(eventHandler);
              queue.Enqueue(responseHandler);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => queue.Dequeue(), h => stubMessage);

              Assert.AreEqual(stubMessage, handler.HandledMessage);
              Assert.AreEqual(stubMessage, commandHandler.HandledMessage);
              Assert.AreEqual(stubMessage, eventHandler.HandledMessage);
              Assert.AreEqual(stubMessage, responseHandler.HandledMessage);
          }

          [Test]
          public void Should_have_passed_through_context()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(StubCommandHandler));
              cache.RegisterHandler(typeof(StubEventHandler));
              cache.RegisterHandler(typeof(StubResponseHandler));

              var commandContext = new StubCommandContext();
              var eventContext = new StubEventContext();
              var responseContext = new StubResponseContext();
              var contextQueue = new Queue<object>();
              contextQueue.Enqueue(commandContext);
              contextQueue.Enqueue(eventContext);
              contextQueue.Enqueue(responseContext);

              var commandHandler = new StubCommandHandler();
              var eventHandler = new StubEventHandler();
              var responseHandler = new StubResponseHandler();
              var handlerQueue = new Queue<object>();
              handlerQueue.Enqueue(commandHandler);
              handlerQueue.Enqueue(eventHandler);
              handlerQueue.Enqueue(responseHandler);

              var handlers = cache.GetHandlersFor(typeof(StubMessage));
              handlers.InvokeAll(h => handlerQueue.Dequeue(), h => new StubMessage(), h => contextQueue.Dequeue());

              Assert.AreEqual(commandContext, commandHandler.MessageContext);
              Assert.AreEqual(eventContext, eventHandler.Context);
              Assert.AreEqual(responseContext, responseHandler.Context);
          }

          class StubHandler : IHandleMessages<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;

              public void Handle(StubMessage message)
              {
                  HandleCalled = true;
                  HandledMessage = message;
              }
          }

          class StubCommandHandler : IProcessCommands<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;
              public ICommandContext MessageContext;

              public void Handle(StubMessage message, ICommandContext context)
              {
                  HandleCalled = true;
                  HandledMessage = message;
                  MessageContext = context;
              }
          }

          class StubEventHandler : IProcessEvents<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;
              public IEventContext Context;

              public void Handle(StubMessage message, IEventContext context)
              {
                  HandleCalled = true;
                  HandledMessage = message;
                  Context = context;
              }
          }

          class StubResponseHandler : IProcessResponses<StubMessage>
          {
              public bool HandleCalled;
              public StubMessage HandledMessage;
              public IResponseContext Context;

              public void Handle(StubMessage message, IResponseContext context)
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

          class StubHandlerNewStyle : IProcessTimeouts<StubTimeoutState>
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

              Assert.AreEqual(12, handlers.Count());
              Assert.AreEqual(1, newStyle.HandleCalled);
              Assert.AreEqual(1, newStyle.HandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.HandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.CommandHandleCalled);
              Assert.AreEqual(1, newStyle.CommandHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.CommandHandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.EventHandleCalled);
              Assert.AreEqual(1, newStyle.EventHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.EventHandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.ResponseHandleCalled);
              Assert.AreEqual(1, newStyle.ResponseHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.ResponseHandleOverloadIMessageCalled);
          }

          class WorstCaseHandlerNewStyle : IProcessCommands<StubMessage>, IProcessCommands<IStubMessage>, IProcessCommands<IMessage>, IProcessEvents<StubMessage>, IProcessEvents<IStubMessage>, IProcessEvents<IMessage>, IProcessResponses<StubMessage>, IProcessResponses<IStubMessage>, IProcessResponses<IMessage>, IHandleMessages<StubMessage>, IHandleMessages<IStubMessage>, IHandleMessages<IMessage>
          {
              public int HandleCalled;
              public int HandleOverloadIStubMessageCalled;
              public int HandleOverloadIMessageCalled;
              public int CommandHandleCalled;
              public int CommandHandleOverloadIStubMessageCalled;
              public int CommandHandleOverloadIMessageCalled;
              public int EventHandleCalled;
              public int EventHandleOverloadIStubMessageCalled;
              public int EventHandleOverloadIMessageCalled;
              public int ResponseHandleCalled;
              public int ResponseHandleOverloadIStubMessageCalled;
              public int ResponseHandleOverloadIMessageCalled;

              public void Handle(StubMessage state, ICommandContext context)
              {
                  CommandHandleCalled++;
              }

              public void Handle(StubMessage message, IEventContext context)
              {
                  EventHandleCalled++;
              }

              public void Handle(StubMessage message)
              {
                  HandleCalled++;
              }

              public void Handle(IStubMessage message)
              {
                  HandleOverloadIStubMessageCalled++;
              }

              public void Handle(IMessage message)
              {
                  HandleOverloadIMessageCalled++;
              }

              public void Handle(IStubMessage message, ICommandContext context)
              {
                  CommandHandleOverloadIStubMessageCalled++;
              }

              public void Handle(IMessage message, ICommandContext context)
              {
                  CommandHandleOverloadIMessageCalled++;
              }

              public void Handle(IStubMessage message, IEventContext context)
              {
                  EventHandleOverloadIStubMessageCalled++;
              }

              public void Handle(IMessage message, IEventContext context)
              {
                  EventHandleOverloadIMessageCalled++;
              }

              public void Handle(StubMessage message, IResponseContext context)
              {
                  ResponseHandleCalled++;
              }

              public void Handle(IStubMessage message, IResponseContext context)
              {
                  ResponseHandleOverloadIStubMessageCalled++;
              }

              public void Handle(IMessage message, IResponseContext context)
              {
                  ResponseHandleOverloadIMessageCalled++;
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
                  case HandlerKind.Command:
                      context = new StubCommandContext();
                      break;
                  case HandlerKind.Event:
                      context = new StubEventContext();
                      break;
                  case HandlerKind.Message:
                      context = new StubResponseContext();
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
      class StubEventContext : IEventContext { }
      class StubResponseContext : IResponseContext { }
      class StubCommandContext : ICommandContext {
          public void Return<T>(T errorEnum)
          {
          }

          public void Reply(object message)
          {
          }

          public ICallback Send(string destination, object message)
          {
              return default(ICallback);
          }

          public void DoNotContinueDispatchingCurrentMessageToHandlers()
          {
          }

          public void HandleCurrentMessageLater()
          {
          }
      }
  }

