using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NUnit.Framework;

namespace NServiceBus.Serializers.Json.Tests
{
  public class A : IMessage
  {
    public byte[] Data;
    public string S;
    public int I { get; set; }

    public DateTime DateTime { get; set; }

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
    public string Bstr { get; set; }
  }

  public abstract class JsonMessageSerializerTestBase
  {
    protected abstract JsonMessageSerializerBase Serializer { get; set; }
    protected MessageMapper MessageMapper { get; private set; }

    protected JsonMessageSerializerTestBase()
    {
      MessageMapper = new MessageMapper();
      MessageMapper.Initialize(new[] { typeof(IA), typeof(A) });
    }

    [Test]
    public void Test()
    {
      var obj = new A
                  {
                    Data = new byte[32],
                    I = 23,
                    S = "Foo",
                    Ints = new List<int> { 12, 42 },
                    Bs = new List<B> { new B { Bstr = "aaa" }, new B { Bstr = "bbbb" } },
                    DateTime = new DateTime(2010, 10, 13, 12, 32, 42)
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

      Assert.AreEqual(a.Data, obj.Data);
      Assert.AreEqual(23, a.I);
      Assert.AreEqual("Foo", a.S);
      Assert.AreEqual(new DateTime(2010, 10, 13, 12, 32, 42), a.DateTime);
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
          x.B = new B { Bstr = "BOO" };
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
      Assert.AreEqual("BOO", a.B.Bstr);
    }

    [Test]
    public void TestMany()
    {
      var output = new MemoryStream();

      var obj = MessageMapper.CreateInstance<IA>(
        x =>
        {
          x.S = "kalle";
          x.I = 42;
          x.Data = new byte[23];
          x.B = new B { Bstr = "BOO" };
        });

      var obj2 = MessageMapper.CreateInstance<IA>(
        x =>
        {
          x.S = "kalle";
          x.I = 42;
          x.Data = new byte[23];
          x.B = new B { Bstr = "BOO" };
        });

      new Random().NextBytes(obj.Data);

      Serializer.Serialize(new IMessage[] { obj, obj2 }, output);

      output.Position = 0;

      var filename = string.Format("{0}.{1}.txt", GetType().Name, MethodBase.GetCurrentMethod().Name);

      File.WriteAllBytes(filename, output.ToArray());

      output.Position = 0;

      var result = Serializer.Deserialize(output);

      Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

      Assert.IsNotEmpty(result);
      Assert.That(result, Has.Length.EqualTo(2));

      Assert.That(result[0], Is.AssignableTo(typeof(IA)));
      var a = ((IA)result[0]);

      Assert.AreEqual(a.Data, obj.Data);
      Assert.AreEqual(42, a.I);
      Assert.AreEqual("kalle", a.S);
      Assert.IsNotNull(a.B);
      Assert.AreEqual("BOO", a.B.Bstr);
    }
  }
}