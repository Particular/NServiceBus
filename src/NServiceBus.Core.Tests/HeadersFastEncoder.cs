namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;

    public class HeadersFastEncoderTests
    {
        [TestCaseSource(nameof(HeaderValues))]
        public void Test(Dictionary<string, string> values)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                HeadersFastEncoder.Write(values, ms);
                bytes = ms.ToArray();
            }

            var actual = HeadersFastEncoder.Read(new ArraySegment<byte>(bytes));

            CollectionAssert.AreEqual(values.OrderBy(kvp => kvp.Key), actual.OrderBy(kvp => kvp.Key), new KeyValuePairComparer());
        }

        [TestCaseSource(nameof(HeaderValues))]
        public void LengthVsJSON(Dictionary<string, string> values)
        {
            using (var ms = new MemoryStream())
            {
                HeadersFastEncoder.Write(values, ms);
                Console.WriteLine($"Fast Encoder payload size: {ms.Position} ");
            }

            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
                serializer.WriteObject(ms,values);
                Console.WriteLine($"JSON Encoded payload size: {ms.Position} ");
            }
        }

        static IEnumerable<ITestCaseData> HeaderValues()
        {
            yield return new TestCaseData(new Dictionary<string, string>()).SetName("Empty");
            yield return new TestCaseData(new Dictionary<string, string> { { "test", "" } }).SetName("Empty value");
            yield return new TestCaseData(new Dictionary<string, string> { { "k1", "v1" }, { "k2", "v2" } }).SetName("Two values");
            yield return new TestCaseData(new Dictionary<string, string>
            {
                { Headers.ContentType, "a" },
                { Headers.CorrelationId, "F497B557-CAD7-42A3-AD53-8015899BA82E" },
                { Headers.SagaId, "BF165325-AD4B-4901-84C2-7B7FA40A5622" },
                { Headers.HostId, "host id" },

            }).SetName("Core headers");
        }

        class KeyValuePairComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var kvp1 = (KeyValuePair<string, string>)x;
                var kvp2 = (KeyValuePair<string, string>)y;

                var comparison = string.Compare(kvp1.Key, kvp2.Key, StringComparison.Ordinal);
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.Compare(kvp1.Value, kvp2.Value, StringComparison.Ordinal);
            }
        }
    }
}