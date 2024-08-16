namespace NServiceBus.Core.Tests.SystemTextJson;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NServiceBus;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serializers.SystemJson;
using NUnit.Framework;

[TestFixture]
public class JsonMessageSerializerTest
{
    public JsonMessageSerializerTest()
    {
        messageMapper = new MessageMapper();
        messageMapper.Initialize(new[] { typeof(IA), typeof(A) });
    }

    JsonMessageSerializer serializer;
    MessageMapper messageMapper;

    [SetUp]
    public void Setup()
    {
        var options = new JsonSerializerOptions();

        serializer = new JsonMessageSerializer(options, ContentTypes.Json, messageMapper);
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
            Ints =
                            [
                                12,
                                42
                            ],
            Bs =
                        [
                            new B
                            {
                                BString = "aaa",
                                C = new C
                                {
                                    Cstr = "ccc"
                                }
                            },
                            new BB
                            {
                                BString = "bbbb",
                                C = new C
                                {
                                    Cstr = "dddd"
                                },
                                BBString = "BBStr"
                            }
                        ],
            DateTime = expectedDate,
            DateTimeLocal = expectedDateLocal,
            DateTimeUtc = expectedDateUtc
        };

        new Random().NextBytes(obj.Data);

        var output = new MemoryStream();

        serializer.Serialize(obj, output);

        var json = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine(json);

        output.Position = 0;

        var result = serializer.Deserialize(output.ToArray(), new[]
                                                    {
                                                    typeof(A)
                                                });

        Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

        Assert.That(result[0], Is.TypeOf(typeof(A)));
        var a = (A)result[0];

        Assert.Multiple(() =>
        {
            Assert.That(a.Data, Is.EqualTo(obj.Data));
            Assert.That(a.I, Is.EqualTo(23));
            Assert.That(a.S, Is.EqualTo("Foo"));
            Assert.That(a.DateTime.Kind, Is.EqualTo(expectedDate.Kind));
            Assert.That(a.DateTime, Is.EqualTo(expectedDate));
            Assert.That(a.DateTimeLocal.Kind, Is.EqualTo(expectedDateLocal.Kind));
            Assert.That(a.DateTimeLocal, Is.EqualTo(expectedDateLocal));
            Assert.That(a.DateTimeUtc.Kind, Is.EqualTo(expectedDateUtc.Kind));
            Assert.That(a.DateTimeUtc, Is.EqualTo(expectedDateUtc));
            Assert.That(a.Bs[0].C, Is.TypeOf(typeof(JsonElement)));
            Assert.That(((JsonElement)a.Bs[0].C).GetProperty("Cstr").GetString(), Is.EqualTo("ccc"));

            Assert.That(a.AGuid, Is.EqualTo(expectedGuid));

            Assert.That(a.Bs[0], Is.InstanceOf<B>());
            Assert.That(a.Bs[1], Is.Not.InstanceOf<BB>());
        });
    }

    [Test]
    public void TestInterfaces()
    {
        var output = new MemoryStream();

        var obj = messageMapper.CreateInstance<IA>(
            x =>
            {
                x.S = "kalle";
                x.I = 42;
                x.Data = new byte[23];
                x.B = new B
                {
                    BString = "BOO",
                    C = new C
                    {
                        Cstr = "COO"
                    }
                };
            }
            );

        new Random().NextBytes(obj.Data);

        messageMapper = new MessageMapper();
        messageMapper.Initialize(new[]
                                    {
                                    typeof(IA), typeof(AImplementation)
                                });

        serializer.Serialize(obj, output);

        output.Position = 0;

        var filename = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}.txt";

        File.WriteAllBytes(filename, output.ToArray());

        output.Position = 0;

        var result = serializer.Deserialize(output.ToArray(), new[]
                                                    {
                                                    typeof(AImplementation)
                                                });

        Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Length.EqualTo(1));

        Assert.That(result[0], Is.AssignableTo(typeof(IA)));
        var a = (IA)result[0];

        Assert.Multiple(() =>
        {
            Assert.That(obj.Data, Is.EqualTo(a.Data));
            Assert.That(a.I, Is.EqualTo(42));
            Assert.That(a.S, Is.EqualTo("kalle"));
            Assert.That(a.B, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(a.B.BString, Is.EqualTo("BOO"));
            Assert.That(a.B.C, Is.TypeOf(typeof(JsonElement)));
            Assert.That(((JsonElement)a.B.C).GetProperty("Cstr").GetString(), Is.EqualTo("COO"));
        });

    }

    [Test]
    public void Should_preserve_timezones()
    {
        var expectedDateTime = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Unspecified);
        var expectedDateTimeLocal = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Local);
        var expectedDateTimeUtc = new DateTime(2010, 10, 13, 12, 32, 42, DateTimeKind.Utc);
        var expectedDateTimeOffset = new DateTimeOffset(2012, 12, 12, 12, 12, 12, TimeSpan.FromHours(6));
#pragma warning disable PS0023 // DateTime.UtcNow or DateTimeOffset.UtcNow should be used instead of DateTime.Now and DateTimeOffset.Now, unless the value is being used for displaying the current date-time in a user's local time zone
        var expectedDateTimeOffsetLocal = DateTimeOffset.Now;
#pragma warning restore PS0023 // DateTime.UtcNow or DateTimeOffset.UtcNow should be used instead of DateTime.Now and DateTimeOffset.Now, unless the value is being used for displaying the current date-time in a user's local time zone
        var expectedDateTimeOffsetUtc = DateTimeOffset.UtcNow;

        using var stream = new MemoryStream();
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

        var result = serializer.Deserialize(stream.ToArray(),
        [
            typeof(DateTimeMessage)
        ]).Cast<DateTimeMessage>().Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.DateTime.Kind, Is.EqualTo(expectedDateTime.Kind));
            Assert.That(result.DateTime, Is.EqualTo(expectedDateTime));
            Assert.That(result.DateTimeLocal.Kind, Is.EqualTo(expectedDateTimeLocal.Kind));
            Assert.That(result.DateTimeLocal, Is.EqualTo(expectedDateTimeLocal));
            Assert.That(result.DateTimeUtc.Kind, Is.EqualTo(expectedDateTimeUtc.Kind));
            Assert.That(result.DateTimeUtc, Is.EqualTo(expectedDateTimeUtc));

            Assert.That(result.DateTimeOffset, Is.EqualTo(expectedDateTimeOffset));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DateTimeOffset.Offset, Is.EqualTo(expectedDateTimeOffset.Offset));
            Assert.That(result.DateTimeOffsetLocal, Is.EqualTo(expectedDateTimeOffsetLocal));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.DateTimeOffsetLocal.Offset, Is.EqualTo(expectedDateTimeOffsetLocal.Offset));
            Assert.That(result.DateTimeOffsetUtc, Is.EqualTo(expectedDateTimeOffsetUtc));
        });
        Assert.That(result.DateTimeOffsetUtc.Offset, Is.EqualTo(expectedDateTimeOffsetUtc.Offset));
    }

    [Test]
    public void Should_be_able_to_deserialize_payloads_with_BOM()
    {
        var payload = JsonSerializer.Serialize(new SomeMessage());

        using var stream = new MemoryStream();
        using var sw = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        sw.WriteLine(payload);
        sw.Flush();

        stream.Position = 0;

        Assert.DoesNotThrow(() => serializer.Deserialize(stream.ToArray(), [typeof(SomeMessage)]));
    }
}

public interface IMyEvent
{
}

public class A : IMessage
{
    public Guid AGuid { get; set; }
    public byte[] Data { get; set; }
    public string S { get; set; }
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
public class AImplementation : IA
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

class SomeMessage
{
}