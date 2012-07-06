namespace NServiceBus.Serializers.XML.Test
{
    using System;
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
                                   Bools = new List<bool> {true, false},
                                   Chars = new List<char> {'a', 'b', 'c', 'd', 'e', 'f'},
                                   Bytes = new List<byte> {byte.MinValue, byte.MaxValue, 11, 1, 1, 0},
                                   Ints = new List<int> {int.MinValue, int.MaxValue, 1, 2, 3, 4, 5, 6},
                                   Decimals =
                                       new List<decimal> {decimal.MinValue, decimal.MaxValue, .2m, 4m, .5m, .4234m},
                                   Doubles =
                                       new List<double>
                                           {double.MinValue, double.MaxValue, .223d, 234d, .513d, .4212334d},
                                   Floats =
                                       new List<float> {float.MinValue, float.MaxValue, .223f, 234f, .513f, .4212334f},
                                   Enums = new List<DateTimeStyles>
                                               {
                                                   DateTimeStyles.AdjustToUniversal,
                                                   DateTimeStyles.AllowLeadingWhite,
                                                   DateTimeStyles.AllowTrailingWhite
                                               },
                                   Longs =
                                       new List<long>
                                           {long.MaxValue, long.MinValue, 34234, 234324, 45345345, 34534534565},
                                   SBytes = new List<sbyte> {sbyte.MaxValue, sbyte.MaxValue, 56, 13},
                                   Shorts = new List<short> {short.MinValue, short.MaxValue, 5231, 6123},
                                   Strings = new List<string> {"Key1", "Value1", "Key2", "Value2", "Key3", "Value3"},
                                   UInts = new List<uint> {uint.MinValue, 23, uint.MaxValue, 34324},
                                   ULongs = new List<ulong> {ulong.MinValue, ulong.MaxValue, 34324234, 3243243245},
                                   UShorts = new List<ushort> {ushort.MinValue, ushort.MaxValue, 42324, 32}
                               };

            var result = ExecuteSerializer.ForMessage<MessageWithLists>(expected);

            CollectionAssert.AreEqual(expected.Bools, result.Bools);
            CollectionAssert.AreEqual(expected.Chars, result.Chars);
            CollectionAssert.AreEqual(expected.Bytes, result.Bytes);
            CollectionAssert.AreEqual(expected.Ints, result.Ints);
            CollectionAssert.AreEqual(expected.Decimals, result.Decimals);
            CollectionAssert.AreEqual(expected.Doubles, result.Doubles);
            CollectionAssert.AreEqual(expected.Floats, result.Floats);
            CollectionAssert.AreEqual(expected.Enums, result.Enums);
            CollectionAssert.AreEqual(expected.Longs, result.Longs);
            CollectionAssert.AreEqual(expected.SBytes, result.SBytes);
            CollectionAssert.AreEqual(expected.Shorts, result.Shorts);
            CollectionAssert.AreEqual(expected.Strings, result.Strings);
            CollectionAssert.AreEqual(expected.UInts, result.UInts);
            CollectionAssert.AreEqual(expected.ULongs, result.ULongs);
            CollectionAssert.AreEqual(expected.UShorts, result.UShorts);
        }
    }

    [Serializable]
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
}