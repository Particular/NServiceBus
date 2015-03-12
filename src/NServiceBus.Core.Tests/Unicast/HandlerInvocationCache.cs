 ﻿namespace NServiceBus.Unicast.Tests
  {
      using System;
      using System.Linq;
      using NServiceBus.Saga;
      using NServiceBus.Unicast.Behaviors;
      using NUnit.Framework;

      //[TestFixture]
      //[Explicit("Performance Tests")]
      //public class HandlerInvocationCachePerformanceTests
      //{
      //    [Test]
      //    public void RunNew()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubMessageHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubTimeoutHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubMessageHandlerNewStyle));
      //        cache.RegisterHandler(typeof(StubTimeoutHandlerNewStyle));
      //        cache.RegisterHandler(typeof(StubSubscribe));

      //        var handler1 = new StubMessageHandlerOldStyle();
      //        var handler2 = new StubTimeoutHandlerOldStyle();
      //        var handler3 = new StubMessageHandlerNewStyle();
      //        var handler4 = new StubTimeoutHandlerNewStyle();
      //        var handler5 = new StubSubscribe();

      //        var stubMessage1 = new StubMessage();
      //        var timeoutState = new StubTimeoutState();
      //        var timeoutContext = new StubTimeoutContext();
      //        var handleContext = new StubHandleContext();
      //        var subscribeContext = new StubSubscribeContext();

      //        cache.Invoke(handler1, stubMessage1, handleContext);
      //        cache.Invoke(handler2, timeoutState, timeoutContext);
      //        cache.Invoke(handler3, stubMessage1, handleContext);
      //        cache.Invoke(handler4, timeoutState, timeoutContext);
      //        cache.Invoke(handler5, stubMessage1, subscribeContext);

      //        var startNew = Stopwatch.StartNew();
      //        for (var i = 0; i < 100000; i++)
      //        {
      //            cache.Invoke(handler1, stubMessage1, handleContext);
      //            cache.Invoke(handler2, timeoutState, timeoutContext);
      //            cache.Invoke(handler3, stubMessage1, handleContext);
      //            cache.Invoke(handler4, timeoutState, timeoutContext);
      //            cache.Invoke(handler5, stubMessage1, subscribeContext);
      //        }
      //        startNew.Stop();
      //        Trace.WriteLine(startNew.ElapsedMilliseconds);
      //    }

      //    class StubMessageHandlerOldStyle : IHandleMessages<StubMessage>
      //    {

      //        public void Handle(StubMessage message)
      //        {
      //        }
      //    }

      //    class StubMessageHandlerNewStyle : IHandle<StubMessage>
      //    {

      //        public void Handle(StubMessage message, IHandleContext context)
      //        {
      //        }
      //    }

      //    class StubSubscribe : ISubscribe<StubMessage>
      //    {

      //        public void Handle(StubMessage message, ISubscribeContext context)
      //        {
      //        }
      //    }

      //    class StubMessage
      //    {
      //    }

      //    class StubTimeoutHandlerOldStyle : IHandleTimeouts<StubTimeoutState>
      //    {
      //        public void Timeout(StubTimeoutState state)
      //        {
      //        }
      //    }

      //    class StubTimeoutHandlerNewStyle : IHandleTimeout<StubTimeoutState>
      //    {
      //        public void Timeout(StubTimeoutState state, ITimeoutContext context)
      //        {
      //        }
      //    }

      //    class StubTimeoutState
      //    {
      //    }
      //}

      //[TestFixture]
      //public class When_invoking_a_cached_handler
      //{
      //    [Test]
      //    public void Should_invoke_handle_method()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));
      //        cache.RegisterHandler(typeof(StubSubscribe));

      //        var oldStyle = new StubHandlerOldStyle();
      //        cache.Invoke(oldStyle, new StubMessage(), new StubHandleContext());

      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, new StubMessage(), new StubHandleContext());

      //        var newStyleSubscribe = new StubSubscribe();
      //        cache.Invoke(newStyleSubscribe, new StubMessage(), new StubSubscribeContext());

      //        Assert.IsTrue(oldStyle.HandleCalled);
      //        Assert.IsTrue(newStyle.HandleCalled);
      //        Assert.IsTrue(newStyleSubscribe.HandleCalled);
      //    }

      //    [Test]
      //    public void Should_have_passed_through_correct_message()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));
      //        cache.RegisterHandler(typeof(StubSubscribe));

      //        var stubMessage = new StubMessage();

      //        var oldStyle = new StubHandlerOldStyle();
      //        cache.Invoke(oldStyle, stubMessage, new StubHandleContext());

      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, stubMessage, new StubHandleContext());

      //        var newStyleSubscribe = new StubSubscribe();
      //        cache.Invoke(newStyleSubscribe, stubMessage, new StubSubscribeContext());

      //        Assert.AreEqual(stubMessage, oldStyle.HandledMessage);
      //        Assert.AreEqual(stubMessage, newStyle.HandledMessage);
      //        Assert.AreEqual(stubMessage, newStyleSubscribe.HandledMessage);
      //    }

      //    [Test]
      //    public void Should_have_passed_through_context()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));
      //        cache.RegisterHandler(typeof(StubSubscribe));

      //        var handleContext = new StubHandleContext();
      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, new StubMessage(), handleContext);

      //        var subscribeContext = new StubSubscribeContext();
      //        var newStyleSubscribe = new StubSubscribe();
      //        cache.Invoke(newStyleSubscribe, new StubMessage(), subscribeContext);

      //        Assert.AreEqual(handleContext, newStyle.Context);
      //        Assert.AreEqual(subscribeContext, newStyleSubscribe.Context);
      //    }

      //    class StubHandlerOldStyle : IHandleMessages<StubMessage>
      //    {
      //        public bool HandleCalled;
      //        public StubMessage HandledMessage;

      //        public void Handle(StubMessage message)
      //        {
      //            HandleCalled = true;
      //            HandledMessage = message;
      //        }
      //    }

      //    class StubHandlerNewStyle : IHandle<StubMessage>
      //    {
      //        public bool HandleCalled;
      //        public StubMessage HandledMessage;
      //        public IHandleContext Context;

      //        public void Handle(StubMessage message, IHandleContext context)
      //        {
      //            HandleCalled = true;
      //            HandledMessage = message;
      //            Context = context;
      //        }
      //    }

      //    class StubSubscribe : ISubscribe<StubMessage>
      //    {
      //        public bool HandleCalled;
      //        public StubMessage HandledMessage;
      //        public ISubscribeContext Context;

      //        public void Handle(StubMessage message, ISubscribeContext context)
      //        {
      //            HandleCalled = true;
      //            HandledMessage = message;
      //            Context = context;
      //        }
      //    }

      //    class StubMessage
      //    {
      //    }
      //}

      //[TestFixture]
      //public class When_invoking_a_cached_timeout_handler
      //{
      //    [Test]
      //    public void Should_invoke_timeout_method()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));

      //        var oldStyle = new StubHandlerOldStyle();
      //        cache.Invoke(oldStyle, new StubTimeoutState(), new StubTimeoutContext());

      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, new StubTimeoutState(), new StubTimeoutContext());

      //        Assert.IsTrue(oldStyle.TimeoutCalled);
      //        Assert.IsTrue(newStyle.TimeoutCalled);
      //    }

      //    [Test]
      //    public void Should_have_passed_through_correct_state()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerOldStyle));
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));

      //        var stubState = new StubTimeoutState();

      //        var oldStyle = new StubHandlerOldStyle();
      //        cache.Invoke(oldStyle, stubState, new StubTimeoutContext());

      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, stubState, new StubTimeoutContext());

      //        Assert.AreEqual(stubState, oldStyle.HandledState);
      //        Assert.AreEqual(stubState, newStyle.HandledState);
      //    }

      //    [Test]
      //    public void Should_have_passed_through_context()
      //    {
      //        var cache = new MessageHandlerRegistry(new Conventions());
      //        cache.RegisterHandler(typeof(StubHandlerNewStyle));

      //        var timeoutContext = new StubTimeoutContext();

      //        var newStyle = new StubHandlerNewStyle();
      //        cache.Invoke(newStyle, new StubTimeoutState(), timeoutContext);

      //        Assert.AreEqual(timeoutContext, newStyle.Context);
      //    }

      //    class StubHandlerOldStyle : IHandleTimeouts<StubTimeoutState>
      //    {
      //        public bool TimeoutCalled;
      //        public StubTimeoutState HandledState;

      //        public void Timeout(StubTimeoutState state)
      //        {
      //            TimeoutCalled = true;
      //            HandledState = state;
      //        }
      //    }

      //    class StubHandlerNewStyle : IHandleTimeout<StubTimeoutState>
      //    {
      //        public bool TimeoutCalled;
      //        public StubTimeoutState HandledState;
      //        public ITimeoutContext Context;

      //        public void Timeout(StubTimeoutState state, ITimeoutContext context)
      //        {
      //            TimeoutCalled = true;
      //            HandledState = state;
      //            Context = context;
      //        }
      //    }

      //    class StubTimeoutState
      //    {
      //    }
      //}

      [TestFixture]
      public class Foo
      {
          [Test]
          public void Should_invoke_timeout_method()
          {
              var cache = new MessageHandlerRegistry(new Conventions());
              cache.RegisterHandler(typeof(WorstCaseHandlerNewStyle));

              var handlers = cache.GetHandlersFor(typeof(StubMessage)).ToList();

              var newStyle = new WorstCaseHandlerNewStyle();
              foreach (var handler in handlers)
              {
                  handler.Instance = newStyle;
                  object context;
                  switch (handler.HandlerKind)
                  {
                      case HandlerKind.Message:
                          context = new StubHandleContext();
                          break;
                      case HandlerKind.Event:
                          context = new StubSubscribeContext();
                          break;
                      default:
                          throw new Exception();
                  }

                  handler.Invoke(new StubMessage(), context);
              }

              Assert.AreEqual(5, handlers.Count());
              Assert.AreEqual(1, newStyle.OldStyleHandleCalled);
              Assert.AreEqual(1, newStyle.OldStyleHandleOverloadIStubMessageCalled);
              Assert.AreEqual(1, newStyle.OldStyleHandleOverloadIMessageCalled);
              Assert.AreEqual(1, newStyle.NewStyleHandleCalled);
              Assert.AreEqual(1, newStyle.SubscribeCalled);
          }

          class WorstCaseHandlerNewStyle : IHandle<StubMessage>, ISubscribe<StubMessage>, IHandleMessages<StubMessage>, IHandleMessages<IStubMessage>, IHandleMessages<IMessage>
          {
              public int OldStyleHandleCalled;
              public int OldStyleHandleOverloadIStubMessageCalled;
              public int OldStyleHandleOverloadIMessageCalled;
              public int NewStyleHandleCalled;
              public int SubscribeCalled;

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
          }

          interface IStubMessage : IMessage { }

          class StubMessage : IStubMessage
          {
          }
      }

      class StubTimeoutContext : ITimeoutContext { }
      class StubSubscribeContext : ISubscribeContext { }
      class StubHandleContext : IHandleContext { }
  }
 
