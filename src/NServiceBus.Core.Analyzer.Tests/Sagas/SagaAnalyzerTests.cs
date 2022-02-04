namespace NServiceBus.Core.Analyzer.Tests.Sagas
{
    using System.Threading.Tasks;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class SagaAnalyzerTests : AnalyzerTestFixture<SagaAnalyzer>
    {
        [Test]
        public Task InfoDiagnosticForSingleOldMapping()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void [|ConfigureHowToFindSaga|](SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga(saga => saga.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.SagaMappingExpressionCanBeRewrittenId, source);
        }

        [Test]
        public Task IAmStartedBySagaNotMappedMsg1()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, [|IAmStartedByMessages<Msg1>|], NServiceBus.IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.MessageStartsSagaButNoMappingId, source);
        }

        [Test]
        public Task IAmStartedBySagaNotMappedMsg2()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, [|NServiceBus.IAmStartedByMessages<Msg2>|]
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
    public string OtherId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.MessageStartsSagaButNoMappingId, source);
        }

        [Test]
        public Task SagaDataPropertyHasNonPublicSetter()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId);
    }
}
public partial class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string [|NoSetter|] { get; }
    public string [|PrivateSet|] { get; private set; }
}
partial class MyData
{
    public string [|InternalSet|] { get; internal set; }
    public string [|ProtectedSet|] { get; protected set; }
    public string [|ProtectedInternalSet|] { get; protected internal set; }
}
";

            return Assert(SagaDiagnostics.SagaDataPropertyNotWriteableId, source);
        }

        [Test]
        public Task MessageMappingNotNeededForTimeouts()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
using MyNS;
public class MySaga : Saga<MyData>, IHandleTimeouts<Timeout1>, IHandleTimeouts<Timeout2>, IHandleTimeouts<Timeout3>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<[|MyNS.Timeout1|]>(msg => msg.UnnecessaryCorrId)
            .ToMessage<[|Timeout2|]>(msg => msg.UnnecessaryCorrId)
            .ToMessage<[|Timeout3|]>(msg => msg.UnnecessaryCorrId);
    }
    public Task Timeout(Timeout1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Timeout(Timeout2 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Timeout(Timeout3 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
namespace MyNS
{
public class Timeout1
{
    public string UnnecessaryCorrId { get; set; }
}
public class Timeout2
{
    public string UnnecessaryCorrId { get; set; }
}
public class Timeout3
{
    public string UnnecessaryCorrId { get; set; }
}
}";

            return Assert(SagaDiagnostics.MessageMappingNotNeededForTimeoutId, source);
        }

        [Test]
        [TestCase("id", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("ID", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("Id", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("iD", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        public Task CannotMapToSagasIdPropertyNewSyntax(string propertyName, string diagnosticId)
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, NServiceBus.IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga([|saga => saga." + propertyName + @"|])
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string " + propertyName + @" { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(diagnosticId, source);
        }

        [Test]
        [TestCase("id", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("ID", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("Id", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        [TestCase("iD", SagaDiagnostics.CannotMapToSagasIdPropertyId)]
        public Task CannotMapToSagasIdPropertyOldSyntax(string propertyName, string diagnosticId)
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, NServiceBus.IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>(msg => msg.CorrId).ToSaga([|saga => saga." + propertyName + @"|]);
        mapper.ConfigureMapping<Msg2>(msg => msg.CorrId).ToSaga([|saga => saga." + propertyName + @"|]);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string " + propertyName + @" { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            var diagnostics = new[] { diagnosticId };
            var ignoreDiagnostics = new[] { SagaDiagnostics.SagaMappingExpressionCanBeSimplifiedId };
            return Assert(diagnostics, source, ignoreDiagnostics);
        }

        [Test]
        [TestCase("Msg1")]
        [TestCase("Msg1[]")]
        [TestCase("List<Msg1>")]
        [TestCase("IEnumerable<Msg1>")]
        [TestCase("ICollection<Msg1>")]
        [TestCase("IDictionary<string, Msg1>")]
        public Task DoNotUseMessageTypeAsSagaDataProperty(string propertyType)
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public [|" + propertyType + @"|] MessageData { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.DoNotUseMessageTypeAsSagaDataPropertyId, source);
        }

        // Valid cases
        [TestCase("string")]
        [TestCase("Guid")]
        [TestCase("long")]
        [TestCase("ulong")]
        [TestCase("int")]
        [TestCase("uint")]
        [TestCase("short")]
        [TestCase("ushort")]
        // Invalid cases
        [TestCase("[|DateTime|]")]
        [TestCase("[|DateTimeOffset|]")]
        [TestCase("[|byte|]")]
        [TestCase("[|IntPtr|]")]
        public Task CorrelationIdShouldBeSupportedType(string correlationPropertyType)
        {
            var typeNoBrackets = correlationPropertyType.Replace("[|", "").Replace("|]", "");

            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public " + correlationPropertyType + @" CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public " + typeNoBrackets + @" CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.CorrelationIdMustBeSupportedTypeId, source);
        }

        [Test]
        public Task EasierToInheritContainSagaData()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public partial class MyData : [|IContainSagaData|]
{
    public string CorrId { get; set; }
}
public partial class MyData
{
    public Guid Id { get; set; }
    public string Originator { get; set; }
    public string OriginalMessageId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.EasierToInheritContainSagaDataId, source);
        }



        [Test]
        public Task RidiculousPartialClassExample()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public partial class MySaga : Saga<MyData>
{
}
public partial class MySaga
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
    }
}
public partial class MySaga : [|IAmStartedByMessages<Msg1>|]
{
}
public partial class MySaga
{
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public partial class MySaga : [|IAmStartedByMessages<Msg2>|]
{
}
public partial class MySaga
{
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1
{
    public string CorrId { get; set; }
}
public class Msg2
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.MessageStartsSagaButNoMappingId, source);
        }

        [Test]
        public Task ShouldUseReplyToOriginator()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public async Task Handle(Msg1 message, IMessageHandlerContext context)
    {
        await [|context.Reply(new ReplyMsg())|];
    }
    private async Task OtherMethod(IMessageHandlerContext ctx)
    {
        for (var i = 0; i < 10; i++)
        {
            await [|ctx.Reply(new ReplyMsg())|];
        }
        // These two are OK
        await ReplyToOriginator(ctx, new ReplyMsg());
        await this.ReplyToOriginator(ctx, new ReplyMsg());
    }
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class ReplyMsg : IMessage {}";

            return Assert(SagaDiagnostics.SagaReplyShouldBeToOriginatorId, source);
        }

        [Test]
        public Task IntermediateBaseClass1()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : [|IntermediateAbstractSaga<MyData>|], IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public abstract class IntermediateAbstractSaga<TSagaData> : Saga<TSagaData> where TSagaData : class, IContainSagaData, new()
{
    protected void SomeMethod() {}
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
}
public class ReplyMsg : IMessage {}";

            return Assert(SagaDiagnostics.SagaShouldNotHaveIntermediateBaseClassId, source);
        }

        [Test]
        public Task IntermediateBaseClass2()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : [|IntermediateAbstractSaga|], IAmStartedByMessages<Msg1>
{
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public abstract class IntermediateAbstractSaga : Saga<MyData>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>(msg => msg.CorrId);
    }
    protected void SomeMethod() {}
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
}
public class ReplyMsg : IMessage {}";

            return Assert(SagaDiagnostics.SagaShouldNotHaveIntermediateBaseClassId, source);
        }

        [Test]
        public Task SagaShouldNotImplementNotFoundHandler()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Sagas;
public partial class MySaga : Saga<MyData>, [|IHandleSagaNotFound|]
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper) { }
    public Task Handle(object message, IMessageProcessingContext context) => throw new NotImplementedException();
}
public partial class MySaga : [|IHandleSagaNotFound|] { }
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.SagaShouldNotImplementNotFoundHandlerId, source);
        }

        [Test]
        public Task ToSagaMappingsMustPointToProperties()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga([|saga => saga.CorrId|])
            .ToMessage<Msg1>(msg => msg.CorrId)
            .ToMessage<Msg2>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId;
}
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.ToSagaMappingMustBeToAPropertyId, source);
        }

        [Test]
        public Task CorrelationExpressionsMustMatchTypeOldSyntax()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>, IAmStartedByMessages<Msg3>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.ConfigureMapping<Msg1>([|msg => msg.CorrId|]).ToSaga(saga => saga.CorrId);
        mapper.ConfigureMapping<Msg2>([|msg => msg.CorrId|]).ToSaga(saga => saga.CorrId);
        mapper.ConfigureHeaderMapping<Msg3>(""CorrId"").ToSaga(saga => saga.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg3 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public int CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public Guid CorrId { get; set; }
}
public class Msg3 : ICommand {}";
            var expected = new[] { SagaDiagnostics.CorrelationPropertyTypeMustMatchMessageMappingExpressionsId };
            var ignore = new[] { SagaDiagnostics.SagaMappingExpressionCanBeSimplifiedId };

            return Assert(expected, source, ignore);
        }

        [Test]
        public Task CorrelationExpressionsMustMatchTypeNewSyntax()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, IAmStartedByMessages<Msg2>, IAmStartedByMessages<Msg3>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId)
            .ToMessage<Msg1>([|msg => msg.CorrId|])
            .ToMessage<Msg2>([|msg => msg.CorrId|])
            .ToMessageHeader<Msg3>(""CorrId"");
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg3 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
}
public class Msg1 : ICommand
{
    public int CorrId { get; set; }
}
public class Msg2 : ICommand
{
    public Guid CorrId { get; set; }
}
public class Msg3 : ICommand {}";

            return Assert(SagaDiagnostics.CorrelationPropertyTypeMustMatchMessageMappingExpressionsId, source);
        }

        [Test]
        public Task MessageTypeMappedToHandlerAndTimeout()
        {
            var source =
@"using System;
using System.Threading.Tasks;
using NServiceBus;
public class MessageWithSagaIdSaga : Saga<MessageWithSagaIdSaga.MessageWithSagaIdSagaData>,
    IAmStartedByMessages<MessageWithSagaId>,
    IHandleTimeouts<MessageWithSagaId>
{
    public Task Handle(MessageWithSagaId message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Timeout(MessageWithSagaId state, IMessageHandlerContext context) => throw new NotImplementedException();
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MessageWithSagaIdSagaData> mapper)
    {
        mapper.MapSaga(s => s.DataId)
            .ToMessage<MessageWithSagaId>(m => m.DataId);
    }
    public class MessageWithSagaIdSagaData : ContainSagaData
    {
        public virtual Guid DataId { get; set; }
    }
}
public class MessageWithSagaId : IMessage
{
    public Guid DataId { get; set; }
}";

            // Should not light up mapping for timeout because it's needed for handler
            return Assert(source);
        }

        [Test]
        public Task StayAwayFromAbstractSagaConstructions()
        {
            var source =
@"
using System;
using NServiceBus;
public abstract class AbstractSaga<TSagaData> : Saga
    where TSagaData : IContainSagaData, new()
{
    protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper) => throw new NotImplementedException();
}";
            // Similar to SQL Persistence SqlSaga<T>
            return Assert(source);
        }

        [Test]
        public Task IgnoreSqlPersistenceSqlSaga()
        {
            var source =
@"
using System;
using NServiceBus;
namespace MyCode
{
    using NServiceBus.Persistence.Sql;
    public class MySaga : SqlSaga<MyData>
    {
    }
    public class MyData : ContainSagaData
    {
        public string CorrId { get; set; }
    }
}
namespace NServiceBus.Persistence.Sql
{
    // Same structure as SQL Persistence SqlSaga (enough of it anyway)
    public abstract class SqlSaga<TSagaData> : Saga
        where TSagaData : IContainSagaData, new()
    {
        protected override void ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage mapper) => throw new NotImplementedException();
    }
}";

            return Assert(source);
        }

        [Test]
        public Task ClassesInSeparateFilesAnalyzeSaga()
        {
            var source =
@"----- Saga code to validate
using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId).ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
-----
using NServiceBus;
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
-----
using NServiceBus;
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(source);
        }

        [Test]
        public Task ClassesInSeparateFilesAnalyzeSagaWithDiagnostic()
        {
            var source =
@"----- Saga code to validate
using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>, [|IAmStartedByMessages<Msg2>|]
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId).ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
    public Task Handle(Msg2 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
-----
using NServiceBus;
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
-----
using NServiceBus;
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
-----
using NServiceBus;
public class Msg2 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(SagaDiagnostics.MessageStartsSagaButNoMappingId, source);
        }

        [Test]
        public Task ClassesInSeparateFilesAnalyzeData()
        {
            var source =
@"using NServiceBus;
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
-----
using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId).ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
-----
using NServiceBus;
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}";

            return Assert(source);
        }

        [Test]
        public Task ClassesInSeparateFilesAnalyzeMessage()
        {
            var source =
@"using NServiceBus;
public class Msg1 : ICommand
{
    public string CorrId { get; set; }
}
-----
using System;
using System.Threading.Tasks;
using NServiceBus;
public class MySaga : Saga<MyData>, IAmStartedByMessages<Msg1>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MyData> mapper)
    {
        mapper.MapSaga(saga => saga.CorrId).ToMessage<Msg1>(msg => msg.CorrId);
    }
    public Task Handle(Msg1 message, IMessageHandlerContext context) => throw new NotImplementedException();
}
-----
using NServiceBus;
public class MyData : ContainSagaData
{
    public string CorrId { get; set; }
    public string OtherId { get; set; }
}
";

            return Assert(source);
        }
    }
}
