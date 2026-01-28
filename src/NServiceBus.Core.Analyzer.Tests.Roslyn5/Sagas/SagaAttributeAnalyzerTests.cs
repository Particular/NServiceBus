#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NServiceBus.Core.Analyzer.Sagas;
using NUnit.Framework;

[TestFixture]
public class SagaAttributeAnalyzerTests : AnalyzerTestFixture<SagaAttributeAnalyzer>
{
    [Test]
    public Task ReportsMissingAttributeOnLeafSaga()
    {
        var source =
            """
            using NServiceBus;

            public class [|OrderShippingPolicy|] : Saga<OrderShippingPolicyData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeMissing, source);
    }

    [Test]
    public Task DoesNotReportWhenAttributePresent()
    {
        var source =
            """
            using NServiceBus;

            [SagaAttribute]
            public class OrderShippingPolicy : Saga<OrderShippingPolicyData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMissingAttributeOnDerivedLeafSaga()
    {
        var source =
            """
            using NServiceBus;

            public abstract class BaseShippingSaga : Saga<OrderShippingPolicyData>
            {
            }

            public class [|OrderShippingPolicy|] : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeMissing, source);
    }

    [Test]
    public Task ReportsMissingAttributeOnNestedLeafSaga()
    {
        var source =
            """
            using NServiceBus;

            public abstract class BaseShippingSaga : Saga<OrderShippingPolicyData>
            {
            }
            
            public class OuterClass
            {
                public class [|OrderShippingPolicy|] : BaseShippingSaga
                {
                    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                    {
                    }
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeMissing, source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnNonSaga()
    {
        var source =
            """
            using NServiceBus;

            [[|SagaAttribute|]]
            class NonSaga
            {
            }
            
            [[|SagaAttribute|]]
            class NonSagaSaga : Saga
            {
                protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
                {
                }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeOnNonSaga, source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|SagaAttribute|]]
            class NonSagaSaga : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }
            
            class MyMessage : IMessage
            {
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeOnNonSaga, source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnAbstractBase()
    {
        var source =
            """
            using NServiceBus;
            
            [[|SagaAttribute|]]
            public abstract class BaseShippingSaga : Saga<OrderShippingPolicyData>
            {
            }

            [SagaAttribute]
            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeMisplaced, source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnComplexBase()
    {
        var source =
            """
            using NServiceBus;
            
            [[|SagaAttribute|]]
            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            [[|SagaAttribute|]]
            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            [SagaAttribute]
            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class OrderShippingPolicyData : ContainSagaData
            {
                public string OrderId { get; set; }
            }
            """;

        return Assert(DiagnosticIds.SagaAttributeMisplaced, source);
    }
}