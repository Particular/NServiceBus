namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using NUnit.Framework;

    [TestFixture]
    public class DictionaryTests
    {
        [Test]
        public void Should_deserialize_dictionaries()
        {
            var expected = new MessageWithDictionaries
                               {
                                   Bools = new Dictionary<bool, bool>
                                               {
                                                   {true, true},
                                                   {false, false}
                                               },
                                   Chars = new Dictionary<char, char>
                                               {
                                                   //{char.MinValue, char.MaxValue}, // doesn't work because we use UTF8
                                                   {'a', 'b'},
                                                   {'c', 'd'},
                                                   {'e', 'f'}
                                               },
                                   Bytes = new Dictionary<byte, byte>
                                               {
                                                   {byte.MinValue, byte.MaxValue},
                                                   {11, 1},
                                                   {1, 0}
                                               },
                                   Ints = new Dictionary<int, int>
                                              {
                                                  {int.MinValue, int.MaxValue},
                                                  {1, 2},
                                                  {3, 4},
                                                  {5, 6}
                                              },
                                   Decimals = new Dictionary<decimal, decimal>
                                                  {
                                                      {decimal.MinValue, decimal.MaxValue},
                                                      {.2m, 4m},
                                                      {.5m, .4234m}
                                                  },
                                   Doubles = new Dictionary<double, double>
                                                 {
                                                     {double.MinValue, double.MaxValue},
                                                     {.223d, 234d},
                                                     {.513d, .4212334d}
                                                 },
                                   Floats = new Dictionary<float, float>
                                                {
                                                    {float.MinValue, float.MaxValue},
                                                    {.223f, 234f},
                                                    {.513f, .4212334f}
                                                },
                                   Enums = new Dictionary<DateTimeStyles, DateTimeKind>
                                               {
                                                   {DateTimeStyles.AdjustToUniversal, DateTimeKind.Local},
                                                   {DateTimeStyles.AllowLeadingWhite, DateTimeKind.Unspecified},

                                               },
                                   Longs = new Dictionary<long, long>
                                               {
                                                   {long.MaxValue, long.MinValue},
                                                   {34234, 234324},
                                                   {45345345, 34534534565}
                                               },
                                   SBytes = new Dictionary<sbyte, sbyte>
                                                {
                                                    {sbyte.MaxValue, sbyte.MaxValue},
                                                    {56, 13}
                                                },
                                   Shorts = new Dictionary<short, short>
                                                {
                                                    {short.MinValue, short.MaxValue},
                                                    {5231, 6123}
                                                },
                                   Strings = new Dictionary<string, string>
                                                 {
                                                     {"Key1", "Value1"},
                                                     {"Key2", "Value2"},
                                                     {"Key3", "Value3"},
                                                 },
                                   UInts = new Dictionary<uint, uint>
                                               {
                                                   {uint.MinValue, 23},
                                                   {uint.MaxValue, 34324}
                                               },
                                   ULongs = new Dictionary<ulong, ulong>
                                                {
                                                    {ulong.MinValue, ulong.MaxValue},
                                                    {34324234, 3243243245}
                                                },
                                   UShorts = new Dictionary<ushort, ushort>
                                                 {
                                                     {ushort.MinValue, ushort.MaxValue},
                                                     {42324, 32}
                                                 }

                               };

            var result = ExecuteSerializer.ForMessage<MessageWithDictionaries>(expected);

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
    public class MessageWithDictionaries : IMessage
    {
        public Dictionary<bool, bool> Bools { get; set; }
        public Dictionary<byte, byte> Bytes { get; set; }
        public Dictionary<char, char> Chars { get; set; }
        public Dictionary<decimal, decimal> Decimals { get; set; }
        public Dictionary<double, double> Doubles { get; set; }
        public Dictionary<DateTimeStyles, DateTimeKind> Enums { get; set; }
        public Dictionary<float, float> Floats { get; set; }
        public Dictionary<int, int> Ints { get; set; }
        public Dictionary<long, long> Longs { get; set; }
        public Dictionary<sbyte, sbyte> SBytes { get; set; }
        public Dictionary<short, short> Shorts { get; set; }
        public Dictionary<uint, uint> UInts { get; set; }
        public Dictionary<ulong, ulong> ULongs { get; set; }
        public Dictionary<ushort, ushort> UShorts { get; set; }
        public Dictionary<string, string> Strings { get; set; }
    }
}