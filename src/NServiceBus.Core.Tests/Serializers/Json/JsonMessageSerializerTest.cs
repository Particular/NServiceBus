namespace NServiceBus.Serializers.Json.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class JsonMessageSerializerTest
    {
        [Test]
        public void Should_handle_concrete_message_with_invalid_interface_property()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(MessageWithInvalidInterfaceProperty)
            });

            var serializer = new JsonMessageSerializer(messageMapper);

            var message = new MessageWithInvalidInterfaceProperty
            {
                InterfaceProperty = new InvalidInterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                var result = (MessageWithInvalidInterfaceProperty) serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithInvalidInterfaceProperty)
                })[0];

                Assert.AreEqual(message.InterfaceProperty.SomeProperty, result.InterfaceProperty.SomeProperty);
            }
        }

        [Test]
        public void Should_handle_concrete_message_with_interface_property()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(MessageWithInterfaceProperty)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            var message = new MessageWithInterfaceProperty
            {
                InterfaceProperty = new InterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                var result = (MessageWithInterfaceProperty) serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithInterfaceProperty)
                })[0];

                Assert.AreEqual(message.InterfaceProperty.SomeProperty, result.InterfaceProperty.SomeProperty);
            }
        }

        [Test]
        public void Should_handle_interface_message_with_interface_property()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(IMessageWithInterfaceProperty)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            IMessageWithInterfaceProperty message = new InterfaceMessageWithInterfacePropertyImplementation
            {
                InterfaceProperty = new InterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                var result = (IMessageWithInterfaceProperty) serializer.Deserialize(stream, new[]
                {
                    typeof(IMessageWithInterfaceProperty)
                })[0];

                Assert.AreEqual(message.InterfaceProperty.SomeProperty, result.InterfaceProperty.SomeProperty);
            }
        }

        [Test]
        public void Deserialize_messages_wrapped_in_array_from_older_endpoint()
        {
            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            var jsonWithMultipleMessages = @"
[
  {
    $type: 'NServiceBus.Serializers.Json.Tests.JsonMessageSerializerTest+SimpleMessage1, NServiceBus.Core.Tests',
    PropertyOnMessage1: 'Message1'
  },
  {
    $type: 'NServiceBus.Serializers.Json.Tests.JsonMessageSerializerTest+SimpleMessage2, NServiceBus.Core.Tests',
    PropertyOnMessage2: 'Message2'
  }
]";
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(jsonWithMultipleMessages);
                streamWriter.Flush();
                stream.Position = 0;
                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(SimpleMessage2),
                    typeof(SimpleMessage1)
                });

                Assert.AreEqual(2, result.Length);
                Assert.AreEqual("Message1", ((SimpleMessage1) result[0]).PropertyOnMessage1);
                Assert.AreEqual("Message2", ((SimpleMessage2) result[1]).PropertyOnMessage2);
            }
        }

        [Test]
        public void Deserialize_message_with_interface_without_wrapping()
        {
            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new SuperMessage
                {
                    SomeProperty = "John"
                }, stream);

                stream.Position = 0;

                var result = (SuperMessage) serializer.Deserialize(stream, new[]
                {
                    typeof(SuperMessage),
                    typeof(IMyEvent)
                })[0];

                Assert.AreEqual("John", result.SomeProperty);
            }
        }

        [Test]
        public void Deserialize_private_message_with_two_unrelated_interface_without_wrapping()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(IMyEventA),
                typeof(IMyEventB)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            using (var stream = new MemoryStream())
            {
                var msg = new CompositeMessage
                {
                    IntValue = 42,
                    StringValue = "Answer"
                };

                serializer.Serialize(msg, stream);

                stream.Position = 0;

                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(IMyEventA),
                    typeof(IMyEventB)
                });
                var a = (IMyEventA) result[0];
                var b = (IMyEventB) result[1];
                Assert.AreEqual(42, b.IntValue);
                Assert.AreEqual("Answer", a.StringValue);
            }
        }

        [Test]
        public void Serialize_message_without_wrapping()
        {
            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new SimpleMessage(), stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.StartsWith("["), result);
            }
        }

        [Test]
        public void Deserialize_message_without_wrapping()
        {
            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new SimpleMessage
                {
                    SomeProperty = "test"
                }, stream);

                stream.Position = 0;
                var result = (SimpleMessage) serializer.Deserialize(stream, new[]
                {
                    typeof(SimpleMessage)
                })[0];

                Assert.AreEqual("test", result.SomeProperty);
            }
        }

        [Test]
        public void Serialize_message_without_typeInfo()
        {
            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new SimpleMessage(), stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.Contains("$type"), result);
            }
        }

        [Test]
        public void Serialize_message_without_concrete_implementation()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(ISuperMessageWithoutConcreteImpl)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageMapper.CreateInstance<ISuperMessageWithoutConcreteImpl>(), stream);

                stream.Position = 0;
                var result = new StreamReader(stream).ReadToEnd();

                Assert.That(!result.Contains("$type"), result);
                Assert.That(result.Contains("SomeProperty"), result);
            }
        }

        [Test]
        public void Deserialize_message_without_concrete_implementation()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(ISuperMessageWithoutConcreteImpl)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            using (var stream = new MemoryStream())
            {
                var msg = messageMapper.CreateInstance<ISuperMessageWithoutConcreteImpl>();
                msg.SomeProperty = "test";

                serializer.Serialize(msg, stream);

                stream.Position = 0;

                var result = (ISuperMessageWithoutConcreteImpl) serializer.Deserialize(stream, new[]
                {
                    typeof(ISuperMessageWithoutConcreteImpl)
                })[0];

                Assert.AreEqual("test", result.SomeProperty);
            }
        }

        [Test]
        public void Deserialize_message_with_concrete_implementation_and_interface()
        {
            var map = new[]
            {
                typeof(SuperMessageWithConcreteImpl),
                typeof(ISuperMessageWithConcreteImpl)
            };
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(map);
            var serializer = new JsonMessageSerializer(messageMapper);

            using (var stream = new MemoryStream())
            {
                var msg = new SuperMessageWithConcreteImpl
                {
                    SomeProperty = "test"
                };

                serializer.Serialize(msg, stream);

                stream.Position = 0;

                var result = (ISuperMessageWithConcreteImpl) serializer.Deserialize(stream, map)[0];

                Assert.IsInstanceOf<SuperMessageWithConcreteImpl>(result);
                Assert.AreEqual("test", result.SomeProperty);
            }
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xml()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument
            {
                Document = XDocument.Load(new StringReader(XmlDocument))
            };
            var messageWithXElement = new MessageWithXElement
            {
                Document = XElement.Load(new StringReader(XmlElement))
            };

            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXDocument, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithXDocument)
                }).Cast<MessageWithXDocument>().Single();

                Assert.AreEqual(messageWithXDocument.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXElement, stream);

                stream.Position = 0;
                var json = new StreamReader(stream).ReadToEnd();
                stream.Position = 0;

                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithXElement)
                }).Cast<MessageWithXElement>().Single();

                Assert.AreEqual(messageWithXElement.Document.ToString(), result.Document.ToString());
                Assert.AreEqual(XmlElement, json.Substring(13, json.Length - 15).Replace("\\", string.Empty));
            }
        }

        [Test]
        public void When_Using_Property_WithNullXContainerAssignable()
        {
            var messageWithXDocument = new MessageWithXDocument
            {
                Document = null
            };

            var messageMapper = new MessageMapper();
            var serializer = new JsonMessageSerializer(messageMapper);
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXDocument, stream);

                stream.Position = 0;

                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithXDocument)
                }).Cast<MessageWithXDocument>().Single();

                Assert.IsNull(result.Document);
            }

            var messageWithXElement = new MessageWithXElement
            {
                Document = null
            };
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXElement, stream);
                stream.Position = 0;

                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(MessageWithXElement)
                }).Cast<MessageWithXElement>().Single();

                Assert.IsNull(result.Document);
            }
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
                Ints = new List<int>
                {
                    12,
                    42
                },
                Bs = new List<B>
                {
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
                }
            };

            new Random().NextBytes(obj.Data);

            var output = new MemoryStream();

            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(IA),
                typeof(A)
            });
            var serializer = new JsonMessageSerializer(messageMapper);

            serializer.Serialize(obj, output);

            output.Position = 0;

            var result = serializer.Deserialize(output, new[]
            {
                typeof(A)
            });

            Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

            Assert.That(result[0], Is.TypeOf(typeof(A)));
            var a = (A) result[0];

            Assert.AreEqual(obj.Data, a.Data);
            Assert.AreEqual(23, a.I);
            Assert.AreEqual("Foo", a.S);
            Assert.AreEqual("ccc", ((C) a.Bs[0].C).Cstr);
            Assert.AreEqual(expectedGuid, a.AGuid);

            Assert.IsInstanceOf<B>(a.Bs[0]);
            Assert.IsInstanceOf<BB>(a.Bs[1]);
        }

        [Test]
        public void TestInterfaces()
        {
            var output = new MemoryStream();

            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(IA),
                typeof(IAImpl)
            });
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

            var serializer = new JsonMessageSerializer(messageMapper);

            serializer.Serialize(obj, output);

            output.Position = 0;

            var filename = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}.txt";

            File.WriteAllBytes(filename, output.ToArray());

            output.Position = 0;

            var result = serializer.Deserialize(output, new[]
            {
                typeof(IAImpl)
            });

            Assert.DoesNotThrow(() => output.Position = 0, "Stream should still be open");

            Assert.IsNotEmpty(result);
            Assert.That(result, Has.Length.EqualTo(1));

            Assert.That(result[0], Is.AssignableTo(typeof(IA)));
            var a = (IA) result[0];

            Assert.AreEqual(a.Data, obj.Data);
            Assert.AreEqual(42, a.I);
            Assert.AreEqual("kalle", a.S);
            Assert.IsNotNull(a.B);
            Assert.AreEqual("BOO", a.B.BString);
            Assert.AreEqual("COO", ((C) a.B.C).Cstr);
        }

        [Test]
        public void TestMany()
        {
            var messageMapper = new MessageMapper();
            messageMapper.Initialize(new[]
            {
                typeof(IAImpl),
                typeof(IA)
            });
            var serializer = new JsonMessageSerializer(messageMapper);
            var xml = @"[{
    $type: ""NServiceBus.Serializers.Json.Tests.IA, NServiceBus.Core.Tests"",
    Data: ""rhNAGU4dr/Qjz6ocAsOs3wk3ZmxHMOg="",
    S: ""kalle"",
    I: 42,
    B: {
        BString: ""BOO"",
        C: {
            $type: ""NServiceBus.Serializers.Json.Tests.C, NServiceBus.Core.Tests"",
            Cstr: ""COO""
        }
    }
}, {
    $type: ""NServiceBus.Serializers.Json.Tests.IA, NServiceBus.Core.Tests"",
    Data: ""AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="",
    S: ""kalle"",
    I: 42,
    B: {
        BString: ""BOO"",
        C: {
            $type: ""NServiceBus.Serializers.Json.Tests.C, NServiceBus.Core.Tests"",
            Cstr: ""COO""
        }
    }
}]";
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(xml);
                streamWriter.Flush();
                stream.Position = 0;


                var result = serializer.Deserialize(stream, new[]
                {
                    typeof(IAImpl)
                });
                Assert.IsNotEmpty(result);
                Assert.That(result, Has.Length.EqualTo(2));

                Assert.That(result[0], Is.AssignableTo(typeof(IA)));
                var a = (IA) result[0];

                Assert.AreEqual(23, a.Data.Length);
                Assert.AreEqual(42, a.I);
                Assert.AreEqual("kalle", a.S);
                Assert.IsNotNull(a.B);
                Assert.AreEqual("BOO", a.B.BString);
                Assert.AreEqual("COO", ((C) a.B.C).Cstr);
            }
        }

        public class SimpleMessage1
        {
            public string PropertyOnMessage1 { get; set; }
        }

        public class SimpleMessage2
        {
            public string PropertyOnMessage2 { get; set; }
        }
    }

    public class SimpleMessage
    {
        public string SomeProperty { get; set; }
    }

    public class SuperMessage : IMyEvent
    {
        public string SomeProperty { get; set; }
    }

    public interface IMyEvent
    {
    }

    public class CompositeMessage : IMyEventA, IMyEventB
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
    }

    public interface IMyEventA
    {
        string StringValue { get; set; }
    }

    public class MyEventA_impl : IMyEventA
    {
        public string StringValue { get; set; }
    }

    public interface IMyEventB
    {
        int IntValue { get; set; }
    }

    public class MyEventB_impl : IMyEventB
    {
        public int IntValue { get; set; }
    }

    public class MessageWithXDocument
    {
        public XDocument Document { get; set; }
    }

    public class MessageWithXElement
    {
        public XElement Document { get; set; }
    }

    public interface ISuperMessageWithoutConcreteImpl : IMyEvent
    {
        string SomeProperty { get; set; }
    }

    public interface ISuperMessageWithConcreteImpl : IMyEvent
    {
        string SomeProperty { get; set; }
    }

    public class SuperMessageWithConcreteImpl : ISuperMessageWithConcreteImpl
    {
        public string SomeProperty { get; set; }
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

    public class IAImpl : IA
    {
        public byte[] Data { get; set; }
        public string S { get; set; }
        public int I { get; set; }
        public B B { get; set; }
    }

    public class A : IMessage
    {
        public Guid AGuid { get; set; }
        public int I { get; set; }

        public List<int> Ints { get; set; }
        public List<B> Bs { get; set; }
        public byte[] Data;
        public string S;
    }

    public interface IA : IMessage
    {
        byte[] Data { get; set; }
        string S { get; set; }
        int I { get; set; }
        B B { get; set; }
    }

    class MessageWithInvalidInterfaceProperty
    {
        public IInvalidInterfaceProperty InterfaceProperty { get; set; }
    }

    public interface IInvalidInterfaceProperty
    {
        string SomeProperty { get; set; }

        void SomeMethod();
    }

    class InvalidInterfacePropertyImplementation : IInvalidInterfaceProperty
    {
        public string SomeProperty { get; set; }

        public void SomeMethod()
        {
        }
    }

    class MessageWithInterfaceProperty
    {
        public IInterfaceProperty InterfaceProperty { get; set; }
    }

    public interface IInterfaceProperty
    {
        string SomeProperty { get; set; }
    }

    class InterfacePropertyImplementation : IInterfaceProperty
    {
        public string SomeProperty { get; set; }
    }

    public interface IMessageWithInterfaceProperty
    {
        IInterfaceProperty InterfaceProperty { get; set; }
    }

    class InterfaceMessageWithInterfacePropertyImplementation : IMessageWithInterfaceProperty
    {
        public IInterfaceProperty InterfaceProperty { get; set; }
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
}