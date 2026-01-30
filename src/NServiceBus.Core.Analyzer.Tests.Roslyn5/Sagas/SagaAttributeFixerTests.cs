#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Analyzer.Sagas;
using Helpers;
using NServiceBus.Core.Analyzer.Fixes;
using NUnit.Framework;

[TestFixture]
public class SagaAttributeFixerTests : CodeFixTestFixture<SagaAttributeAnalyzer, SagaAttributeFixer>
{
    [Test]
    public Task RemoveSagaAttributeOnNonSaga()
    {
        var original =
            """
            using NServiceBus;

            [SagaAttribute]
            class NonSaga
            {
            }
            
            [SagaAttribute]
            class NonSagaSaga : Saga
            {
                protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
                {
                }
            }
            """;

        var expected =
            """
            using NServiceBus;
            
            class NonSaga
            {
            }
            
            class NonSagaSaga : Saga
            {
                protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
                {
                }
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
            [NServiceBus.SagaAttribute, DebuggerNonUserCodeAttribute]
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
    public Task AddsSagaAttributeToLeafSaga()
    {
        var original =
            """
            using NServiceBus;

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

        var expected =
            """
            using NServiceBus;

            [Saga]
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

        return Assert(original, expected);
    }

    [Test]
    public Task MovesSagaAttributeToLeafSaga()
    {
        var original =
            """
            using NServiceBus;
            
            [SagaAttribute]
            public abstract class BaseShippingSaga : Saga<OrderShippingPolicyData>
            {
            }

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

        var expected =
            """
            using NServiceBus;
            
            public abstract class BaseShippingSaga : Saga<OrderShippingPolicyData>
            {
            }

            [Saga]
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

        return Assert(original, expected);
    }

    [Test]
    public Task MovesSagaAttributeToLeafSagaEvenForComplexHierarchies()
    {
        var original =
            """
            using NServiceBus;
            
            [SagaAttribute]
            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            [SagaAttribute]
            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class VipOrderShippingPolicy : BaseShippingSaga
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

        var expected =
            """
            using NServiceBus;

            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            [Saga]
            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            [Saga]
            public class VipOrderShippingPolicy : BaseShippingSaga
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

        return Assert(original, expected);
    }

    [Test]
    public Task MovesHandlerSagaOnBaseSagaToLeafSagaEvenForComplexHierarchies()
    {
        var original =
            """
            using NServiceBus;

            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            [Saga]
            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class VipOrderShippingPolicy : BaseShippingSaga
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

        var expected =
            """
            using NServiceBus;

            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            [Saga]
            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            [Saga]
            public class VipOrderShippingPolicy : BaseShippingSaga
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

        return Assert(original, expected);
    }

    [Test]
    public Task MovesSagaAttributeOnBaseBaseSagaToLeafSagaEvenForComplexHierarchies()
    {
        var original =
            """
            using NServiceBus;

            [Saga]
            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            public class VipOrderShippingPolicy : BaseShippingSaga
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

        var expected =
            """
            using NServiceBus;

            public abstract class BaseBaseSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
            {
            }

            public abstract class BaseShippingSaga : BaseBaseSaga<OrderShippingPolicyData>
            {
            }

            [Saga]
            public class OrderShippingPolicy : BaseShippingSaga
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderShippingPolicyData> mapper)
                {
                }
            }

            [Saga]
            public class VipOrderShippingPolicy : BaseShippingSaga
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

        return Assert(original, expected);
    }
}