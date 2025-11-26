#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests.Sagas;

using System.Threading.Tasks;
using Helpers;
using NServiceBus.Core.Analyzer.Fixes;
using NUnit.Framework;

[TestFixture]
public class RewriteConfigureHowToFindSagaFixerTests : CodeFixTestFixture<SagaAnalyzer, RewriteConfigureHowToFindSagaFixer>
{
    [Test]
    public Task AddMissingMessageMappingFallbackValue()
    {
        var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
}";

        var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.MessagePropertyWithCorrelationValue);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
}";

        return Assert(original, expected, fixMustCompile: false);
    }

    [Test]
    public Task AddMissingMessageMappingWhenPropertyMatchesCorrelation()
    {
        var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

        var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

        return Assert(original, expected);
    }

    /// <summary>
    /// NOTE: Don't need to test when ConfigureHowToFindSaga method is missing completely, because
    /// that's a compile error: the abstract base class method is not implemented
    /// </summary>
    [Test]
    public Task AddMissingMessageMappingWhenMappingMethodIsEmpty()
    {
        var original =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

        var expected =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrelationPropertyName)
            .ToMessage<Msg1>(msg => msg.MessagePropertyWithCorrelationValue);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

        return Assert(original, expected, fixMustCompile: false);
    }
}