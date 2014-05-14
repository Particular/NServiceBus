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

        public DateTime DateTime { get; set; }
        public DateTime DateTimeLocal { get; set; }
        public DateTime DateTimeUtc { get; set; }

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
        public void Test()
        {
            var expectedDate = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
            var expectedDateLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
            var expectedDateUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
            var expectedGuid = Guid.NewGuid();

            var obj = new A
                        {
                            AGuid = expectedGuid,
                            Data = new byte[32],
                            I = 23,
                            S = "Foo",
                            Ints = new List<int> { 12, 42 },
                            Bs = new List<B> { new B { BString = "aaa", C = new C { Cstr = "ccc" } }, new BB { BString = "bbbb", C = new C { Cstr = "dddd" }, BBString = "BBStr"} },
                            DateTime = expectedDate,
                            DateTimeLocal = expectedDateLocal,
                            DateTimeUtc = expectedDateUtc
                        };

            new Random().NextBytes(obj.Data);

            var output = new MemoryStream();

            Serializer.Serialize(new IMessage[] { obj }, output);

            output.Position = 0;

            var filename = string.Format("{0}.{1}.txt", GetType().Name, MethodBase.GetCurrentMethod().Name);

            File.WriteAllBytes(filename, output.ToArray());

            output.Position = 0;

            var result = Serializer.Deserialize(output);

            Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

            Assert.IsNotEmpty(result);
            Assert.That(result, Has.Length.EqualTo(1));

            Assert.That(result[0], Is.TypeOf(typeof(A)));
            var a = ((A)result[0]);

            Assert.AreEqual(obj.Data, a.Data);
            Assert.AreEqual(23, a.I);
            Assert.AreEqual("Foo", a.S);
            Assert.AreEqual(expectedDate.Kind, a.DateTime.Kind);
            Assert.AreEqual(expectedDate, a.DateTime);
            Assert.AreEqual(expectedDateLocal.Kind, a.DateTimeLocal.Kind);
            Assert.AreEqual(expectedDateLocal, a.DateTimeLocal);
            Assert.AreEqual(expectedDateUtc.Kind, a.DateTimeUtc.Kind);
            Assert.AreEqual(expectedDateUtc, a.DateTimeUtc);
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

            Serializer.Serialize(new IMessage[] { obj }, output);

            output.Position = 0;

            var filename = string.Format("{0}.{1}.txt", GetType().Name, MethodBase.GetCurrentMethod().Name);

            File.WriteAllBytes(filename, output.ToArray());

            output.Position = 0;

            var result = Serializer.Deserialize(output);

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