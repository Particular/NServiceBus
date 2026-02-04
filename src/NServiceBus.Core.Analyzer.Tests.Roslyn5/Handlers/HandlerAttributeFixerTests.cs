#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NServiceBus.Core.Analyzer.Fixes;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;

[TestFixture]
public class HandlerAttributeFixerTests : CodeFixTestFixture<HandlerAttributeAnalyzer, HandlerAttributeFixer>
{
    [Test]
    public Task RemoveHandlerAttributeOnNonHandler()
    {
        var original =
            """
            using NServiceBus;

            [Handler]
            class NonHandler
            {
            }
            """;

        var expected =
            """
            using NServiceBus;
            
            class NonHandler
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task PreservesOtherAttributes()
    {
        var original =
            """
            using System.Diagnostics;
            using NServiceBus;

            [StackTraceHiddenAttribute]
            [NServiceBus.HandlerAttribute, DebuggerNonUserCodeAttribute]
            [DebuggerStepThroughAttribute]
            class NonHandler
            {
            }
            """;

        var expected =
            """
            using System.Diagnostics;
            using NServiceBus;

            [StackTraceHiddenAttribute]
            [DebuggerNonUserCodeAttribute]
            [DebuggerStepThroughAttribute]
            class NonHandler
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task RemoveHandlerAttributeOnSaga()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            [Handler]
            class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IHandleMessages<MyMessage>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
                
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            class OrderShippingPolicy : Saga<OrderShippingPolicyData>, IHandleMessages<MyMessage>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
                
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task AddsHandlerAttributeToLeafHandler()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MovesHandlerAttributeToLeafHandler()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class ConcreteHandler : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            [Handler]
            class ConcreteHandler : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task AddsAttributeToLeaveHandlerButLeavesBaseHandlerWhenNonAbstract()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class ConcreteHandler : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            [Handler]
            class ConcreteHandler : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MovesHandlerAttributeToLeafHandlerEvenForComplexHierarchies()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            [Handler]
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }

            [Handler]
            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }

            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }
            
            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }
            
            [Handler]
            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            [Handler]
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MovesHandlerAttributeOnBaseHandlerToLeafHandlerEvenForComplexHierarchies()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }

            [Handler]
            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }

            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }
            
            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }
            
            [Handler]
            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            [Handler]
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MovesHandlerAttributeOnBaseBaseHandlerToLeafHandlerEvenForComplexHierarchies()
    {
        var original =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            [Handler]
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }

            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }

            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }
            
            abstract class BaseHandler : BaseBaseHandler
            {
                public sealed override Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    return HandleCore(message, context);
                }
                
                protected abstract Task HandleCore(MyMessage message, IMessageHandlerContext context);
            }
            
            [Handler]
            class ConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            [Handler]
            class AnotherConcreteHandler : BaseHandler
            {
                protected override Task HandleCore(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        return Assert(original, expected);
    }
}