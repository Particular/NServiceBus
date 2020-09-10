namespace NServiceBus.Persistence.InMemory.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_persisting_saga_data : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_support_basic_member_types()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointThatHostsASaga>(
                    b => b.When(session => session.SendLocal(new StartSaga
                    {
                        CorrelationId = Guid.NewGuid()
                    })))
                .Done(c => c.SagaDataLoaded)
                .Run();

            Assert.NotNull(context.LoadedSagaData);

            CollectionAssert.AreEquivalent(StringArray, context.LoadedSagaData.StringArray);

            CollectionAssert.AreEquivalent(Collection, context.LoadedSagaData.Collection);

            CollectionAssert.AreEquivalent(List, context.LoadedSagaData.List);

            Assert.AreEqual(2, context.LoadedSagaData.IntStringDictionary.Count);
            Assert.AreEqual(IntStringDictionary[1], context.LoadedSagaData.IntStringDictionary[1]);
            Assert.AreEqual(IntStringDictionary[2], context.LoadedSagaData.IntStringDictionary[2]);

            Assert.AreEqual(2, context.LoadedSagaData.IntStringIDictionary.Count);
            Assert.AreEqual(IntStringIDictionary[1], context.LoadedSagaData.IntStringIDictionary[1]);
            Assert.AreEqual(IntStringIDictionary[2], context.LoadedSagaData.IntStringIDictionary[2]);

            Assert.AreEqual(2, context.LoadedSagaData.StringStringDictionary.Count);
            Assert.AreEqual(StringStringDictionary["1"], context.LoadedSagaData.StringStringDictionary["1"]);
            Assert.AreEqual(StringStringDictionary["2"], context.LoadedSagaData.StringStringDictionary["2"]);

            Assert.AreEqual(2, context.LoadedSagaData.StringObjectDictionary.Count);
            Assert.AreEqual(StringObjectDictionary["obj1"].Guid, context.LoadedSagaData.StringObjectDictionary["obj1"].Guid);
            Assert.AreEqual(StringObjectDictionary["obj1"].Int, context.LoadedSagaData.StringObjectDictionary["obj1"].Int);
            Assert.AreEqual(StringObjectDictionary["obj1"].String, context.LoadedSagaData.StringObjectDictionary["obj1"].String);
            Assert.AreEqual(StringObjectDictionary["obj2"].Guid, context.LoadedSagaData.StringObjectDictionary["obj2"].Guid);
            Assert.AreEqual(StringObjectDictionary["obj2"].Int, context.LoadedSagaData.StringObjectDictionary["obj2"].Int);
            Assert.AreEqual(StringObjectDictionary["obj2"].String, context.LoadedSagaData.StringObjectDictionary["obj2"].String);

            Assert.AreEqual(2, context.LoadedSagaData.ReadOnlyDictionary.Count);
            Assert.AreEqual(ReadOnlyDictionary["hello"], context.LoadedSagaData.ReadOnlyDictionary["hello"]);
            Assert.AreEqual(ReadOnlyDictionary["world"], context.LoadedSagaData.ReadOnlyDictionary["world"]);

            Assert.AreEqual(DateTimeLocal, context.LoadedSagaData.DateTimeLocal);
            Assert.AreEqual(DateTimeLocal.Kind, context.LoadedSagaData.DateTimeLocal.Kind);
            Assert.AreEqual(DateTimeUnspecified, context.LoadedSagaData.DateTimeUnspecified);
            Assert.AreEqual(DateTimeUnspecified.Kind, context.LoadedSagaData.DateTimeUnspecified.Kind);
            Assert.AreEqual(DateTimeUtc, context.LoadedSagaData.DateTimeUtc);
            Assert.AreEqual(DateTimeUtc.Kind, context.LoadedSagaData.DateTimeUtc.Kind);

            Assert.AreEqual(DateTimeOffset, context.LoadedSagaData.DateTimeOffset);
            Assert.AreEqual(DateTimeOffset.Offset, context.LoadedSagaData.DateTimeOffset.Offset);
            Assert.AreEqual(DateTimeOffset.LocalDateTime, context.LoadedSagaData.DateTimeOffset.LocalDateTime);
        }

        static string[] StringArray =
        {
            "a",
            "b",
            "c"
        };

        static Dictionary<int, string> IntStringDictionary = new Dictionary<int, string>
        {
            {1, "hello"},
            {2, "world"}
        };

        static Dictionary<string, string> StringStringDictionary = new Dictionary<string, string>
        {
            {"1", "hello"},
            {"2", "world"}
        };

        static IDictionary<int, string> IntStringIDictionary = new Dictionary<int, string>
        {
            {1, "hello"},
            {2, "interface world"}
        };

        static Dictionary<string, SamplePoco> StringObjectDictionary = new Dictionary<string, SamplePoco>
        {
            {
                "obj1", new SamplePoco
                {
                    Guid = Guid.NewGuid(),
                    Int = 21,
                    String = "abc"
                }
            },
            {
                "obj2", new SamplePoco
                {
                    Guid = Guid.NewGuid(),
                    Int = 42,
                    String = "xyz"
                }
            }
        };

        static ICollection<string> Collection = new HashSet<string>
        {
            "1",
            "2"
        };

        static IList<string> List = new List<string>
        {
            "1",
            "2"
        };

        static IReadOnlyDictionary<string,int> ReadOnlyDictionary = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
        {
            {"hello", 11},
            {"world", 22}
        });

        static DateTime DateTimeLocal = new DateTime(2010, 10, 10, 10, 10, 10, DateTimeKind.Local);
        static DateTime DateTimeUnspecified = new DateTime(2010, 10, 10, 10, 10, 10, DateTimeKind.Unspecified);
        static DateTime DateTimeUtc = new DateTime(2010, 10, 10, 10, 10, 10, DateTimeKind.Utc);

        static DateTimeOffset DateTimeOffset = new DateTimeOffset(2010, 10, 10, 10, 10, 10, TimeSpan.FromHours(10));

        public class SamplePoco
        {
            public int Int { get; set; }
            public string String { get; set; }
            public Guid Guid { get; set; }
        }

        public class Context : ScenarioContext
        {
            public SupportedFieldTypesSagaData LoadedSagaData { get; set; }
            public bool SagaDataLoaded { get; set; }
        }

        public class EndpointThatHostsASaga : EndpointConfigurationBuilder
        {
            public EndpointThatHostsASaga()
            {
                EndpointSetup<DefaultServer>();
            }

            public class SupportedFieldTypesSaga : Saga<SupportedFieldTypesSagaData>,
                IAmStartedByMessages<StartSaga>,
                IHandleMessages<LoadTheSagaAgain>
            {
                public SupportedFieldTypesSaga(Context context)
                {
                    testContext = context;
                }

                public Task Handle(StartSaga message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.StringArray = StringArray;
                    Data.Collection = Collection;
                    Data.List = List;
                    Data.IntStringDictionary = IntStringDictionary;
                    Data.IntStringIDictionary = IntStringIDictionary;
                    Data.StringStringDictionary = StringStringDictionary;
                    Data.StringObjectDictionary = StringObjectDictionary;
                    Data.ReadOnlyDictionary = ReadOnlyDictionary;
                    Data.DateTimeLocal = DateTimeLocal;
                    Data.DateTimeUnspecified = DateTimeUnspecified;
                    Data.DateTimeUtc = DateTimeUtc;
                    Data.DateTimeOffset = DateTimeOffset;

                    return context.SendLocal(new LoadTheSagaAgain
                    {
                        DataId = Data.CorrelationId
                    });
                }

                public Task Handle(LoadTheSagaAgain message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.LoadedSagaData = Data;
                    testContext.SagaDataLoaded = true;

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SupportedFieldTypesSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSaga>(m => m.CorrelationId).ToSaga(s => s.CorrelationId);
                    mapper.ConfigureMapping<LoadTheSagaAgain>(m => m.DataId).ToSaga(s => s.CorrelationId);
                }

                Context testContext;
            }
        }

        public class SupportedFieldTypesSagaData : ContainSagaData
        {
            public Guid CorrelationId { get; set; }
            public string[] StringArray { get; set; }
            public Dictionary<int, string> IntStringDictionary { get; set; }
            public IDictionary<int, string> IntStringIDictionary { get; set; }
            public IDictionary<string, string> StringStringDictionary { get; set; }
            public Dictionary<string, SamplePoco> StringObjectDictionary { get; set; }
            public IReadOnlyDictionary<string, int> ReadOnlyDictionary { get; set; }
            public DateTime DateTimeLocal { get; set; }
            public DateTime DateTimeUnspecified { get; set; }
            public DateTime DateTimeUtc { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public ICollection<string> Collection { get; set; }
            public IList<string> List { get; set; }
        }

        public class StartSaga : IMessage
        {
            public Guid CorrelationId { get; set; }
        }

        public class LoadTheSagaAgain : IMessage
        {
            public Guid DataId { get; set; }
        }
    }
}