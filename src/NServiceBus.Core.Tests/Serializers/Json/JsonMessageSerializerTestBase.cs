namespace NServiceBus.Serializers.Json.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    public class A : IMessage
    {
        public Guid AGuid { get; set; }
        public byte[] Data;
        public string S;
        public int I { get; set; }

        public List<int> Ints { get; set; }
        public List<B> Bs { get; set; }

    }

    public interface IA : IMessage
    {
        byte[] Data { get; set; }
        string S { get; set; }
        int I { get; set; }
        B B { get; set; }
    }
    public class IAImpl : IA
    {
        public byte[] Data { get; set; }
        public string S { get; set; }
        public int I { get; set; }
        public B B { get; set; }
    }

    public class B
    {
        public string BString { get; set; }
        public object C { get; set; }
    }

    public class BB : B
    {
        public string BBString { get; set; }
    }

    public class C
    {
        public string Cstr { get; set; }
    }

    class DateTimeMessage
    {
        public DateTime DateTime { get; set; }
        public DateTime DateTimeLocal { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTimeOffset DateTimeOffsetLocal { get; set; }
        public DateTimeOffset DateTimeOffsetUtc { get; set; }
    }

    public abstract class JsonMessageSerializerTestBase
    {
        protected JsonMessageSerializerBase Serializer { get; set; }
        protected MessageMapper MessageMapper { get; set; }

        protected JsonMessageSerializerTestBase(params Type[] messageTypes)
        {
            MessageMapper = new MessageMapper();
            MessageMapper.Initialize(new[] { typeof(IA), typeof(A) }.Union(messageTypes));
        }

        [Test]
        public void Should_preserve_timezones()
        {
            var expectedDateTime = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
            var expectedDateTimeLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
            var expectedDateTimeUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
            var expectedDateTimeOffset = new DateTimeOffset(2012, 12, 12, 12, 12, 12, TimeSpan.FromHours(6));
            var expectedDateTimeOffsetLocal = DateTimeOffset.Now;
            var expectedDateTimeOffsetUtc = DateTimeOffset.UtcNow;

            using (var stream = new MemoryStream())
            {
                var serializer = new JsonMessageSerializer(new MessageMapper());
                serializer.Serialize(new DateTimeMessage
                {
                    DateTime = expectedDateTime,
                    DateTimeLocal = expectedDateTimeLocal,
                    DateTimeUtc = expectedDateTimeUtc,
                    DateTimeOffset = expectedDateTimeOffset,
                    DateTimeOffsetLocal = expectedDateTimeOffsetLocal,
                    DateTimeOffsetUtc = expectedDateTimeOffsetUtc
                }, stream);
                stream.Position = 0;

                var result = serializer.Deserialize(stream, new List<Type>
                {
                    typeof(DateTimeMessage)
                }).Cast<DateTimeMessage>().Single();

                Assert.AreEqual(expectedDateTime.Kind, result.DateTime.Kind);
                Assert.AreEqual(expectedDateTime, result.DateTime);
                Assert.AreEqual(expectedDateTimeLocal.Kind, result.DateTimeLocal.Kind);
                Assert.AreEqual(expectedDateTimeLocal, result.DateTimeLocal);
                Assert.AreEqual(expectedDateTimeUtc.Kind, result.DateTimeUtc.Kind);
                Assert.AreEqual(expectedDateTimeUtc, result.DateTimeUtc);

                Assert.AreEqual(expectedDateTimeOffset, result.DateTimeOffset);
                Assert.AreEqual(expectedDateTimeOffset.Offset, result.DateTimeOffset.Offset);
                Assert.AreEqual(expectedDateTimeOffsetLocal, result.DateTimeOffsetLocal);
                Assert.AreEqual(expectedDateTimeOffsetLocal.Offset, result.DateTimeOffsetLocal.Offset);
                Assert.AreEqual(expectedDateTimeOffsetUtc, result.DateTimeOffsetUtc);
                Assert.AreEqual(expectedDateTimeOffsetUtc.Offset, result.DateTimeOffsetUtc.Offset);
            }

        }

        [Test]
        public void Should_handle_types_correctly()
        {
            var expectedGuid = Guid.NewGuid();

            var obj = new A
                        {
                            AGuid = expectedGuid,
                            Data = new byte[32],
                            I = 23,
                            S = "Foo",
                            Ints = new List<int> { 12, 42 },
                            Bs = new List<B> { new B { BString = "aaa", C = new C { Cstr = "ccc" } }, new BB { BString = "bbbb", C = new C { Cstr = "dddd" }, BBString = "BBStr"} },
                        };

            new Random().NextBytes(obj.Data);

            var output = new MemoryStream();

            MessageMapper = new MessageMapper();
            MessageMapper.Initialize(new[] { typeof(IA), typeof(A) });
            Serializer = new JsonMessageSerializer(MessageMapper);

            Serializer.Serialize(obj, output);

            output.Position = 0;

            var result = Serializer.Deserialize(output, new[] { typeof(A) });

            Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

            Assert.That(result[0], Is.TypeOf(typeof(A)));
            var a = ((A)result[0]);

            Assert.AreEqual(obj.Data, a.Data);
            Assert.AreEqual(23, a.I);
            Assert.AreEqual("Foo", a.S);
            Assert.AreEqual("ccc", ((C)a.Bs[0].C).Cstr);
            Assert.AreEqual(expectedGuid, a.AGuid);

            Assert.IsInstanceOf<B>(a.Bs[0]);
            Assert.IsInstanceOf<BB>(a.Bs[1]);
        }

        [Test]
        public void TestInterfaces()
        {
            var output = new MemoryStream();

            var obj = MessageMapper.CreateInstance<IA>(
              x =>
              {
                  x.S = "kalle";
                  x.I = 42;
                  x.Data = new byte[23];
                  x.B = new B { BString = "BOO", C = new C { Cstr = "COO" } };
              }
              );

            new Random().NextBytes(obj.Data);

            MessageMapper = new MessageMapper();
            MessageMapper.Initialize(new[] { typeof(IA), typeof(IAImpl) });
            Serializer = new JsonMessageSerializer(MessageMapper);

            Serializer.Serialize(obj, output);

            output.Position = 0;

            var filename = string.Format("{0}.{1}.txt", GetType().Name, MethodBase.GetCurrentMethod().Name);

            File.WriteAllBytes(filename, output.ToArray());

            output.Position = 0;

            var result = Serializer.Deserialize(output, new[] { typeof(IAImpl) });

            Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

            Assert.IsNotEmpty(result);
            Assert.That(result, Has.Length.EqualTo(1));

            Assert.That(result[0], Is.AssignableTo(typeof(IA)));
            var a = ((IA)result[0]);

            Assert.AreEqual(a.Data, obj.Data);
            Assert.AreEqual(42, a.I);
            Assert.AreEqual("kalle", a.S);
            Assert.IsNotNull(a.B);
            Assert.AreEqual("BOO", a.B.BString);
            Assert.AreEqual("COO", ((C)a.B.C).Cstr);
        }

    }
}