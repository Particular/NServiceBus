namespace NServiceBus.Serializers.XML.Test;

using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

[TestFixture]
public class ListTests
{
    [Test]
    public void Should_deserialize_list()
    {
        var expected = new MessageWithLists
        {
            Bools = [true, false],
            Chars = ['a', 'b', 'c', 'd', 'e', 'f'],
            Bytes = [byte.MinValue, byte.MaxValue, 11, 1, 1, 0],
            Ints = [int.MinValue, int.MaxValue, 1, 2, 3, 4, 5, 6],
            Decimals =
                                   [decimal.MinValue, decimal.MaxValue, .2m, 4m, .5m, .4234m],
            Doubles =
                                   [double.MinValue, double.MaxValue, .223d, 234d, .513d, .4212334d],
            Floats =
                                   [float.MinValue, float.MaxValue, .223f, 234f, .513f, .4212334f],
            Enums =
                                           [
                                               DateTimeStyles.AdjustToUniversal,
                                               DateTimeStyles.AllowLeadingWhite,
                                               DateTimeStyles.AllowTrailingWhite
                                           ],
            Longs =
                                   [long.MaxValue, long.MinValue, 34234, 234324, 45345345, 34534534565],
            SBytes = [sbyte.MaxValue, sbyte.MaxValue, 56, 13],
            Shorts = [short.MinValue, short.MaxValue, 5231, 6123],
            Strings = ["Key1", "Value1", "Key2", "Value2", "Key3", "Value3"],
            UInts = [uint.MinValue, 23, uint.MaxValue, 34324],
            ULongs = [ulong.MinValue, ulong.MaxValue, 34324234, 3243243245],
            UShorts = [ushort.MinValue, ushort.MaxValue, 42324, 32]
        };

        var result = ExecuteSerializer.ForMessage<MessageWithLists>(expected);

        Assert.That(result.Bools, Is.EqualTo(expected.Bools).AsCollection);
        Assert.That(result.Chars, Is.EqualTo(expected.Chars).AsCollection);
        Assert.That(result.Bytes, Is.EqualTo(expected.Bytes).AsCollection);
        Assert.That(result.Ints, Is.EqualTo(expected.Ints).AsCollection);
        Assert.That(result.Decimals, Is.EqualTo(expected.Decimals).AsCollection);
        Assert.That(result.Doubles, Is.EqualTo(expected.Doubles).AsCollection);
        Assert.That(result.Floats, Is.EqualTo(expected.Floats).AsCollection);
        Assert.That(result.Enums, Is.EqualTo(expected.Enums).AsCollection);
        Assert.That(result.Longs, Is.EqualTo(expected.Longs).AsCollection);
        Assert.That(result.SBytes, Is.EqualTo(expected.SBytes).AsCollection);
        Assert.That(result.Shorts, Is.EqualTo(expected.Shorts).AsCollection);
        Assert.That(result.Strings, Is.EqualTo(expected.Strings).AsCollection);
        Assert.That(result.UInts, Is.EqualTo(expected.UInts).AsCollection);
        Assert.That(result.ULongs, Is.EqualTo(expected.ULongs).AsCollection);
        Assert.That(result.UShorts, Is.EqualTo(expected.UShorts).AsCollection);
    }
}


public class MessageWithLists : IMessage
{
    public List<bool> Bools { get; set; }
    public List<byte> Bytes { get; set; }
    public List<char> Chars { get; set; }
    public List<decimal> Decimals { get; set; }
    public List<double> Doubles { get; set; }
    public List<DateTimeStyles> Enums { get; set; }
    public List<float> Floats { get; set; }
    public List<int> Ints { get; set; }
    public List<long> Longs { get; set; }
    public List<sbyte> SBytes { get; set; }
    public List<short> Shorts { get; set; }
    public List<uint> UInts { get; set; }
    public List<ulong> ULongs { get; set; }
    public List<ushort> UShorts { get; set; }
    public List<string> Strings { get; set; }
}