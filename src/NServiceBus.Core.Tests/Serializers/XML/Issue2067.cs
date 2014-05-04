using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Core.Tests.Serializers.XML
{
    using NServiceBus.Serializers.XML.Test;
    using NUnit.Framework;

    [Serializable]
    class MyMessage
    {
        public KeyValuePair<string, string>[] FailureArgs { get; set; }
    }

    [TestFixture]
    public class Issue2067
    {

        [Test]
        public void CanSerializeNullElements()
        {
            var message = new MyMessage
                          {
                              FailureArgs = new[]
                                            {
                                                new KeyValuePair<string, string>("Arg0", "Value0"),
                                                new KeyValuePair<string, string>("Arg0", "Value0")
                                            }
                          };

            var result = ExecuteSerializer.ForMessage<MyMessage>(message);
            Assert.IsNotNull(result.FailureArgs);
            Assert.AreEqual(2, result.FailureArgs.Count());
        }
    }
}
