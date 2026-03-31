#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class HandlerAttributeAnalyzerTests : AnalyzerTestFixture<HandlerAttributeAnalyzer>
{
    [Test]
    public Task ReportsMissingAttributeOnLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class [|MyHandler|] : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task ReportsMissingAttributeOnNestedLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class OuterClass
            {
                class [|MyHandler|] : IHandleMessages<MyMessage>
                {
                    public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                }
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task DoesNotReportWhenAttributePresent()
    {
        var source =
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

        return Assert(source);
    }

    [Test]
    public Task ReportsMissingAttributeOnConventionBasedLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class [|MyHandler|]
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.ConventionBasedHandlerMissingAttribute);
    }

    [Test]
    public Task DoesNotReportForHelperClassWithNonMessageHandlerContext()
    {
        var source =
            """
            using System.Threading.Tasks;

            class MyHelper
            {
                public Task Handle(MyMessage message, MyContext context) => Task.CompletedTask;
            }

            class MyMessage
            {
            }

            class MyContext
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForHelperClassWithNonPublicHandleMethod()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHelper
            {
                private Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForHelperClassWithSingleParameterHandleMethod()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHelper
            {
                public Task Handle(MyMessage message) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForConventionBasedHandlerReturningValueTask()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler
            {
                public ValueTask Handle(MyMessage message, IMessageHandlerContext context) => ValueTask.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportWhenAttributePresentOnNonAbstractBaseClass()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [HandlerAttribute]
            class BaseHandler : IHandleMessages<MyMessage>
            {
                public virtual Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            [HandlerAttribute]
            class ConcreteHandler : BaseHandler
            {
                public override Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportWhenAttributePresentButHasNonHandlerBaseClass()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;
            
            class HandlerBase
            {
            }

            [Handler]
            class MyHandler : HandlerBase, IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMissingAttributeOnDerivedLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class [|ConcreteHandler|] : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task ReportsInterfaceBasedMissingAttributeWhenDerivedHandlerImplementsInterfaceButBaseProvidesVirtualHandle()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler
            {
                public virtual Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class [|ConcreteHandler|] : BaseHandler, IHandleMessages<MyMessage>
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task ReportMissingAttributeWhenAttributeNotPresentOnNonAbstractBaseClass()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class [|BaseHandler|] : IHandleMessages<MyMessage>
            {
                public virtual Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class [|ConcreteHandler|] : BaseHandler
            {
                public override Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task ReportsWhenMixing()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class [|ConcreteHandler|] : BaseHandler, IHandleMessages<AnotherMessage>
            {
                public Task Handle(AnotherMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            
            class AnotherMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMissing);
    }

    [Test]
    public Task DoesNotReportWhenMixingAndAttributeOnLeaveHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            [Handler]
            class ConcreteHandler : BaseHandler, IHandleMessages<AnotherMessage>
            {
                public Task Handle(AnotherMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }

            class AnotherMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnNonHandler()
    {
        var source =
            """
            using NServiceBus;

            [[|HandlerAttribute|]]
            class NonHandler
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeOnNonHandler);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnSaga()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|HandlerAttribute|]]
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

        return Assert(source, DiagnosticIds.HandlerAttributeOnNonHandler);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnAbstractBase()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|HandlerAttribute|]]
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

        return Assert(source, DiagnosticIds.HandlerAttributeMisplaced);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnConventionBasedAbstractBase()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|HandlerAttribute|]]
            abstract class BaseHandler
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

        return Assert(source, DiagnosticIds.ConventionBasedHandlerMisplacedAttribute);
    }

    [Test]
    public Task ReportsMixedStyleError()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class [|MyHandler|] : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                public Task Handle(AnotherMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService {}

            class MyMessage : IMessage {}
            class AnotherMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.ConventionBasedHandlerMixedStyle);
    }

    [Test]
    public Task DoesNotReportForPureConventionBasedHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService {}

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForConventionBasedHandlerWithOptionalCancellationToken()
    {
        var source =
            """
            using System.Threading;
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public async Task Handle(MyMessage message, IMessageHandlerContext context, CancellationToken cancellationToken = default)
                {
                    await Task.CompletedTask;
                }
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportMixedStyleForPureInterfaceBased()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMixedStyleErrorWhenConventionBasedMethodHasSameMessageTypeButExtraParams()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class [|MyHandler|] : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                public Task Handle(MyMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService {}

            class MyMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.ConventionBasedHandlerMixedStyle);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnComplexBase()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|HandlerAttribute|]]
            abstract class BaseBaseHandler : IHandleMessages<MyMessage>
            {
                public abstract Task Handle(MyMessage message, IMessageHandlerContext context);
            }
            
            [[|HandlerAttribute|]]
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

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source, DiagnosticIds.HandlerAttributeMisplaced);
    }

    [Test]
    public Task ReportsConventionBasedHandlerWithOnlyPrivateConstructors()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class [|MyHandler|]
            {
                private MyHandler() { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.ConventionBasedHandlerNoAccessibleConstructor);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithPublicConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public MyHandler() { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithInternalConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                internal MyHandler() { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithMixedAccessibilityConstructors()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                private MyHandler(string _) { }
                public MyHandler() { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsStaticConventionBasedHandlerEvenWithOnlyStaticHandleMethods()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            static class [|MyHandler|]
            {
                public static Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.HandlerClassCannotBeStatic);
    }

    [Test]
    public Task ReportsConventionBasedHandlerWithAmbiguousConstructors()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class [|MyHandler|]
            {
                public MyHandler(IServiceA serviceA) { }
                public MyHandler(IServiceB serviceB) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            interface IServiceA {}
            interface IServiceB {}
            class MyMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.ConventionBasedHandlerAmbiguousConstructor);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithDifferentLengthConstructors()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public MyHandler() { }
                public MyHandler(IServiceA serviceA) { }
                public MyHandler(IServiceA serviceA, IServiceB serviceB) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            interface IServiceA {}
            interface IServiceB {}
            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithAmbiguousConstructors_MarkedWithActivatorUtilitiesConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public MyHandler(IServiceA serviceA) { }
                [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
                public MyHandler(IServiceB serviceB) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            interface IServiceA {}
            interface IServiceB {}
            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportConventionBasedHandlerWithSingleConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class MyHandler
            {
                public MyHandler(IServiceA serviceA) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            interface IServiceA {}
            class MyMessage : IMessage {}
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsStaticConventionBasedHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            static class [|MyHandler|]
            {
                public static Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage {}
            """;

        return Assert(source, DiagnosticIds.HandlerClassCannotBeStatic);
    }
}
