namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;
    using A;
    using AlternateNamespace;
    using B;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class SerializerTests
    {
        [Test]
        public void SerializeInvalidCharacters()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithInvalidCharacter>();
            var msg = mapper.CreateInstance<MessageWithInvalidCharacter>();

            var sb = new StringBuilder();
            sb.Append("Hello");
            sb.Append((char)0x1C);
            sb.Append("John");
            msg.Special = sb.ToString();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithInvalidCharacter)msgArray[0];
                Assert.That(m.Special, Is.EqualTo(sb.ToString()));
            }
        }

        [Test] //note: This is not a desired behavior, but this test documents this limitation
        public void Limitation_Does_not_handle_types_implementing_ISerializable()
        {
            var message = new MessageImplementingISerializable("test");

            var serializer = SerializerFactory.Create<MessageImplementingISerializable>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);
                stream.Position = 0;

                var result = (MessageImplementingISerializable)serializer.Deserialize(stream.ToArray())[0];

                Assert.That(result.ReadOnlyProperty, Is.Null);
            }
        }

        [Test]
        public void Should_handle_struct_message()
        {
            var message = new StructMessage
            {
                SomeProperty = "property",
                SomeField = "field"
            };

            var serializer = SerializerFactory.Create<StructMessage>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);
                stream.Position = 0;

                var result = (StructMessage)serializer.Deserialize(stream.ToArray())[0];

                Assert.Multiple(() =>
                {
                    Assert.That(result.SomeField, Is.EqualTo(message.SomeField));
                    Assert.That(result.SomeProperty, Is.EqualTo(message.SomeProperty));
                });
            }
        }

        [Test] //note: This is not a desired behavior, but this test documents this limitation
        public void Limitation_Does_not_handle_message_with_struct_property()
        {
            var message = new MessageWithStructProperty();

            var serializer = SerializerFactory.Create<MessageWithStructProperty>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);
                stream.Position = 0;

                var ex = Assert.Throws<Exception>(() => serializer.Deserialize(stream.ToArray()));

                Assert.That(ex.Message, Does.StartWith("Type not supported by the serializer"));
            }
        }

        [Test] //note: This is not a desired behavior, but this test documents this limitation
        public void Limitation_Does_not_handle_concrete_message_with_invalid_interface_property()
        {
            var message = new MessageWithInvalidInterfaceProperty
            {
                InterfaceProperty = new InvalidInterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };
            var serializer = SerializerFactory.Create<MessageWithInvalidInterfaceProperty>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);
                stream.Position = 0;

                Assert.Throws<Exception>(() => serializer.Deserialize(stream.ToArray()));
            }
        }

        [Test]
        public void Should_handle_concrete_message_with_interface_property()
        {
            var message = new MessageWithInterfaceProperty
            {
                InterfaceProperty = new InterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };
            var serializer = SerializerFactory.Create<MessageWithInterfaceProperty>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                var result = (MessageWithInterfaceProperty)serializer.Deserialize(stream.ToArray())[0];

                Assert.That(result.InterfaceProperty.SomeProperty, Is.EqualTo(message.InterfaceProperty.SomeProperty));
            }
        }

        [Test]
        public void Should_handle_interface_message_with_interface_property()
        {
            var message = new InterfaceMessageWithInterfacePropertyImplementation
            {
                InterfaceProperty = new InterfacePropertyImplementation
                {
                    SomeProperty = "test"
                }
            };
            var serializer = SerializerFactory.Create<IMessageWithInterfaceProperty>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                var result = (IMessageWithInterfaceProperty)serializer.Deserialize(stream.ToArray(), new[]
                {
                    typeof(IMessageWithInterfaceProperty)
                })[0];

                Assert.That(result.InterfaceProperty.SomeProperty, Is.EqualTo(message.InterfaceProperty.SomeProperty));
            }
        }

        [Test]
        [Ignore("ArrayList is not supported")]
        public void Should_deserialize_arrayList()
        {
            var expected = new ArrayList
            {
                "Value1",
                "Value2",
                "Value3"
            };
            var result = ExecuteSerializer.ForMessage<MessageWithArrayList>(m3 => m3.ArrayList = expected);

            Assert.That(result.ArrayList, Is.EqualTo(expected).AsCollection);
        }

        [Test]
        [Ignore("Hashtable is not supported")]
        public void Should_deserialize_hashtable()
        {
            var expected = new Hashtable
            {
                {"Key1", "Value1"},
                {"Key2", "Value2"},
                {"Key3", "Value3"}
            };
            var result = ExecuteSerializer.ForMessage<MessageWithHashtable>(m3 => m3.Hashtable = expected);

            Assert.That(result.Hashtable, Is.EqualTo(expected).AsCollection);
        }

        [Test]
        public void Should_deserialize_multiple_messages_from_different_namespaces()
        {
            var xml = @"<?xml version=""1.0"" ?>
<Messages
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
    xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test.A""
    xmlns:q1=""http://tempuri.net/NServiceBus.Serializers.XML.Test.B"">
    <Command1>
        <Id>1eb17e5d-8573-49af-a5cb-76b4a602bb79</Id>
    </Command1>
    <q1:Command2>
        <Id>ad3b5a84-6cf1-4376-aa2d-058b1120c12f</Id>
    </q1:Command2>
</Messages>
";
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(xml);
                writer.Flush();
                stream.Position = 0;
                var msgArray = SerializerFactory.Create(typeof(Command1), typeof(Command2)).Deserialize(stream.ToArray());

                Assert.Multiple(() =>
                {
                    Assert.That(msgArray[0].GetType(), Is.EqualTo(typeof(Command1)));
                    Assert.That(msgArray[1].GetType(), Is.EqualTo(typeof(Command2)));
                });
            }
        }

        [Test]
        public void Should_infer_message_type_from_root_node_if_type_is_known()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<NServiceBus.Serializers.XML.Test.MessageWithDouble><Double>23.4</Double></NServiceBus.Serializers.XML.Test.MessageWithDouble>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream.ToArray());

                Assert.That(msgArray[0].GetType(), Is.EqualTo(typeof(MessageWithDouble)));
            }
        }

        [Test]
        public void Deserialize_private_message_with_two_unrelated_interface_without_wrapping()
        {
            var serializer = SerializerFactory.Create(typeof(CompositeMessage), typeof(IMyEventA), typeof(IMyEventB));
            var deserializer = SerializerFactory.Create(typeof(IMyEventA), typeof(IMyEventB));

            using (var stream = new MemoryStream())
            {
                var msg = new CompositeMessage
                {
                    IntValue = 42,
                    StringValue = "Answer"
                };

                serializer.Serialize(msg, stream);

                stream.Position = 0;

                var result = deserializer.Deserialize(stream.ToArray(), new[]
                {
                    typeof(IMyEventA),
                    typeof(IMyEventB)
                });
                var a = (IMyEventA)result[0];
                var b = (IMyEventB)result[1];
                Assert.Multiple(() =>
                {
                    Assert.That(b.IntValue, Is.EqualTo(42));
                    Assert.That(a.StringValue, Is.EqualTo("Answer"));
                });
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_element()
        {
            Serializer.ForMessage<EmptyMessage>(new EmptyMessage())
                .AssertResultingXml(d => d.DocumentElement.Name == "EmptyMessage", "Root should be message typename");
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_xml_raw_data()
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

            Serializer.ForMessage<MessageWithXDocument>(messageWithXDocument, s => { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");

            Serializer.ForMessage<MessageWithXElement>(messageWithXElement, s => { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");
        }

        [Test]
        public void Should_deserialize_messages_where_xml_raw_data_root_element_matches_property_name()
        {
            const string XmlElement = "<Document xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></Document>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument
            {
                Document = XDocument.Load(new StringReader(XmlDocument))
            };
            var messageWithXElement = new MessageWithXElement
            {
                Document = XElement.Load(new StringReader(XmlElement))
            };

            var serializer = SerializerFactory.Create<MessageWithXDocument>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXDocument, stream);
                stream.Position = 0;

                serializer = SerializerFactory.Create(typeof(MessageWithXDocument));
                serializer.SkipWrappingRawXml = true;

                var msg = serializer.Deserialize(stream.ToArray()).Cast<MessageWithXDocument>().Single();

                Assert.That(msg.Document, Is.Not.Null);
                Assert.That(msg.Document.Root.Name.LocalName, Is.EqualTo("Document"));
            }

            serializer = SerializerFactory.Create<MessageWithXElement>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXElement, stream);
                stream.Position = 0;

                serializer = SerializerFactory.Create(typeof(MessageWithXElement));
                serializer.SkipWrappingRawXml = true;

                var msg = serializer.Deserialize(stream.ToArray()).Cast<MessageWithXElement>().Single();

                Assert.That(msg.Document, Is.Not.Null);
                Assert.That(msg.Document.Name.LocalName, Is.EqualTo("Document"));
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_default_namespaces_and_then_deserialize()
        {
            var serializer = SerializerFactory.Create<MessageWithDouble>();
            var msg = new MessageWithDouble();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream.ToArray());

                Assert.That(msgArray[0].GetType(), Is.EqualTo(typeof(MessageWithDouble)));
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_default_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test""></EmptyMessage>";

            AssertSerializedEquals(serializer, msg, expected);
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_specified_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.Namespace = "http://super.com";
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://super.com/NServiceBus.Serializers.XML.Test""></EmptyMessage>";

            AssertSerializedEquals(serializer, msg, expected);
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_specified_namespace_with_trailing_forward_slashes()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.Namespace = "http://super.com///";
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://super.com/NServiceBus.Serializers.XML.Test""></EmptyMessage>";

            AssertSerializedEquals(serializer, msg, expected);
        }

        static void AssertSerializedEquals(XmlMessageSerializer serializer, IMessage msg, string expected)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                string result;
                using (var reader = new StreamReader(stream))
                {
                    result = XDocument.Load(reader).ToString();
                }

                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public void Should_deserialize_a_single_message_with_typeName_passed_in_externally()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<WhatEver><Double>23.4</Double></WhatEver>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream.ToArray(), new[]
                {
                    typeof(MessageWithDouble)
                });

                Assert.That(msgArray[0].GetType(), Is.EqualTo(typeof(MessageWithDouble)));
            }
        }

        [Test]
        public void Should_deserialize_a_single_message_with_typeName_passed_in_externally_even_when_not_initialized_with_type()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<WhatEver><Double>23.4</Double></WhatEver>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create()
                    .Deserialize(stream.ToArray(), new[]
                    {
                        typeof(MessageWithDouble)
                    });

                Assert.That(((MessageWithDouble)msgArray[0]).Double, Is.EqualTo(23.4));
            }
        }

        [Test]
        public void Should_deserialize_a_batched_messages_with_typeName_passed_in_externally()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<Messages><WhatEver><Double>23.4</Double></WhatEver><TheEmptyMessage></TheEmptyMessage></Messages>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble), typeof(EmptyMessage))
                    .Deserialize(stream.ToArray(), new[]
                    {
                        typeof(MessageWithDouble),
                        typeof(EmptyMessage)
                    });

                Assert.Multiple(() =>
                {
                    Assert.That(((MessageWithDouble)msgArray[0]).Double, Is.EqualTo(23.4));
                    Assert.That(msgArray[1].GetType(), Is.EqualTo(typeof(EmptyMessage)));
                });
            }
        }

        [Test]
        public void Should_deserialize_a_batched_messages_with_typeName_passed_in_externally_even_when_not_initialized_with_type()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<Messages><WhatEver><Double>23.4</Double></WhatEver><TheEmptyMessage></TheEmptyMessage></Messages>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create()
                    .Deserialize(stream.ToArray(), new[]
                    {
                        typeof(MessageWithDouble),
                        typeof(EmptyMessage)
                    });

                Assert.Multiple(() =>
                {
                    Assert.That(((MessageWithDouble)msgArray[0]).Double, Is.EqualTo(23.4));
                    Assert.That(msgArray[1].GetType(), Is.EqualTo(typeof(EmptyMessage)));
                });
            }
        }

        [Test]
        public void TestMultipleInterfacesDuplicatedProperty()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<IThird>(mapper);
            var msgBeforeSerialization = mapper.CreateInstance<IThird>(x => x.FirstName = "Danny");

            var count = 0;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msgBeforeSerialization, stream);
                stream.Position = 0;

                var reader = XmlReader.Create(stream);

                while (reader.Read())
                {
                    if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "FirstName"))
                    {
                        count++;
                    }
                }
            }
            Assert.That(count, Is.EqualTo(1));
        }


        [Test]
        public void Generic_properties_should_be_supported()
        {
            var result = ExecuteSerializer.ForMessage<MessageWithGenericProperty>(m =>
            {
                m.GenericProperty =
                    new GenericProperty<string>("test")
                    {
                        WhatEver = "a property"
                    };
            });

            Assert.That(result.GenericProperty.WhatEver, Is.EqualTo("a property"));
        }


        [Test]
        public void Culture()
        {
            var serializer = SerializerFactory.Create<MessageWithDouble>();
            var val = 65.36;
            var msg = new MessageWithDouble
            {
                Double = val
            };

            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var stream = new MemoryStream();
            serializer.Serialize(msg, stream);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            stream.Position = 0;
            var msgArray = serializer.Deserialize(stream.ToArray());
            var m = (MessageWithDouble)msgArray[0];

            Assert.That(m.Double, Is.EqualTo(val));

            stream.Dispose();
        }

        [Test]
        public void Comparison()
        {
            TestInterfaces();
            TestDataContractSerializer();
        }

        [Test]
        public void TestInterfaces()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<ISecondSerializableMessage>(mapper);


            var o = mapper.CreateInstance<ISecondSerializableMessage>();

            o.Id = Guid.NewGuid();
            o.Age = 10;
            o.Address = Guid.NewGuid().ToString();
            o.Int = 7;
            o.Name = "udi";
            o.Uri = new Uri("http://www.UdiDahan.com/");
            o.Risk = new Risk
            {
                Percent = 0.15D,
                Annum = true,
                Accuracy = 0.314M
            };
            o.Some = SomeEnum.B;
            o.Start = DateTime.UtcNow;
            o.Duration = TimeSpan.Parse("-01:15:27.123");
            o.Offset = DateTimeOffset.UtcNow;
            o.Lookup = new MyDictionary
            {
                ["1"] = "1"
            };
            o.Foos = new Dictionary<string, List<Foo>>
            {
                ["foo1"] = [.. new[]
            {
                new Foo
                {
                    Name = "1",
                    Title = "1"
                },
                new Foo
                {
                    Name = "2",
                    Title = "2"
                }
            }]
            };
            o.Data =
            [
                1,
                2,
                3,
                4,
                5,
                4,
                3,
                2,
                1
            ];
            o.SomeStrings =
            [
                "a",
                "b",
                "c"
            ];

            o.ArrayFoos =
            [
                new Foo
                {
                    Name = "FooArray1",
                    Title = "Mr."
                },
                new Foo
                {
                    Name = "FooAray2",
                    Title = "Mrs"
                }
            ];
            o.Bars =
            [
                new Bar
                {
                    Name = "Bar1",
                    Length = 1
                },
                new Bar
                {
                    Name = "BAr2",
                    Length = 5
                }
            ];
            o.NaturalNumbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

            o.Developers =
            [
                "Udi Dahan",
                "Andreas Ohlund",
                "Matt Burton",
                "Jonathan Oliver et al"
            ];

            o.Parent = mapper.CreateInstance<IFirstSerializableMessage>();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk
            {
                Percent = 0.15D,
                Annum = true,
                Accuracy = 0.314M
            };

            o.Names = [];
            for (var i = 0; i < number; i++)
            {
                var firstMessage = mapper.CreateInstance<IFirstSerializableMessage>();
                o.Names.Add(firstMessage);
                firstMessage.Age = 10;
                firstMessage.Address = Guid.NewGuid().ToString();
                firstMessage.Int = 7;
                firstMessage.Name = i.ToString();
                firstMessage.Risk = new Risk
                {
                    Percent = 0.15D,
                    Annum = true,
                    Accuracy = 0.314M
                };
            }

            o.MoreNames = o.Names.ToArray();

            Time(o, serializer);
        }

        [Test]
        public void TestDataContractSerializer()
        {
            var o = CreateSecondSerializableMessage();
            var messages = new IMessage[]
            {
                o
            };

            var dataContractSerializer = new DataContractSerializer(typeof(ArrayList), new[]
            {
                typeof(SecondSerializableMessage),
                typeof(SomeEnum),
                typeof(FirstSerializableMessage),
                typeof(Risk),
                typeof(List<FirstSerializableMessage>)
            });

            var sw = new Stopwatch();
            sw.Start();

            var xmlWriterSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false
            };

            var xmlReaderSettings = new XmlReaderSettings
            {
                IgnoreProcessingInstructions = true,
                ValidationType = ValidationType.None,
                IgnoreWhitespace = true,
                CheckCharacters = false,
                ConformanceLevel = ConformanceLevel.Auto
            };

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var stream = new MemoryStream())
                {
                    DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, stream);
                }
            }

            sw.Stop();
            Debug.WriteLine("serialization " + sw.Elapsed);

            sw.Reset();

            var fileName = Path.GetTempFileName();
            Console.Error.WriteLine($"{nameof(fileName)}: {fileName}");

            File.Delete(fileName);

            using (var fs = File.Open(fileName, FileMode.OpenOrCreate))
            {
                DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, fs);
            }

            var s = new MemoryStream();
            DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, s);
            var buffer = s.GetBuffer();
            s.Dispose();

            sw.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var reader = XmlReader.Create(new MemoryStream(buffer), xmlReaderSettings))
                {
                    dataContractSerializer.ReadObject(reader);
                }
            }

            sw.Stop();
            Debug.WriteLine("deserializing: " + sw.Elapsed);
        }

        [Test]
        public void SerializeLists()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = mapper.CreateInstance<MessageWithList>();

            msg.Items =
            [
                new MessageWithListItem
                {
                    Data = "Hello"
                }
            ];

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithList)msgArray[0];
                Assert.That(m.Items.First().Data, Is.EqualTo("Hello"));
            }
        }

        [Test]
        public void SerializeClosedGenericListsInAlternateNamespace()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespace>();
            var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespace>();

            msg.Items =
            [
                new MessageWithListItemAlternate
                {
                    Data = "Hello"
                }
            ];

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithClosedListInAlternateNamespace)msgArray[0];
                Assert.That(m.Items.First().Data, Is.EqualTo("Hello"));
            }
        }

        [Test]
        public void SerializeClosedGenericListsInAlternateNamespaceMultipleIEnumerableImplementations()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations>();
            var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations>();

#pragma warning disable IDE0028 // Simplify collection initialization
            msg.Items = new AlternateItemListMultipleIEnumerableImplementations
            {
                new MessageWithListItemAlternate
                {
                    Data = "Hello"
                }
            };
#pragma warning restore IDE0028 // Simplify collection initialization

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations)msgArray[0];
                Assert.That(m.Items.First<MessageWithListItemAlternate>().Data, Is.EqualTo("Hello"));
            }
        }

        [Test]
        public void SerializeClosedGenericListsInAlternateNamespaceMultipleIListImplementations()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIListImplementations>();
            var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespaceMultipleIListImplementations>();

            msg.Items =
            [
                new MessageWithListItemAlternate
                {
                    Data = "Hello"
                }
            ];

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithClosedListInAlternateNamespaceMultipleIListImplementations)msgArray[0];
                Assert.That(m.Items.First<MessageWithListItemAlternate>().Data, Is.EqualTo("Hello"));
            }
        }

        [Test]
        public void SerializeClosedGenericListsInSameNamespace()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithClosedList>();
            var msg = mapper.CreateInstance<MessageWithClosedList>();

            msg.Items =
            [
                new MessageWithListItem
                {
                    Data = "Hello"
                }
            ];

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithClosedList)msgArray[0];
                Assert.That(m.Items.First().Data, Is.EqualTo("Hello"));
            }
        }

        [Test]
        public void SerializeEmptyLists()
        {
            var mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = mapper.CreateInstance<MessageWithList>();

            msg.Items = [];

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream.ToArray());
                var m = (MessageWithList)msgArray[0];
                Assert.That(m.Items, Is.Empty);
            }
        }

        static void DataContractSerialize(XmlWriterSettings xmlWriterSettings, DataContractSerializer dataContractSerializer, IMessage[] messages, Stream stream)
        {
            var o = new ArrayList(messages);
            using (var xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
            {
                dataContractSerializer.WriteStartObject(xmlWriter, o);
                dataContractSerializer.WriteObjectContent(xmlWriter, o);
                dataContractSerializer.WriteEndObject(xmlWriter);
            }
        }

        SecondSerializableMessage CreateSecondSerializableMessage()
        {
            var secondMessage = new SecondSerializableMessage
            {
                Id = Guid.NewGuid(),
                Age = 10,
                Address = Guid.NewGuid().ToString(),
                Int = 7,
                Name = "udi",
                Risk = new Risk
                {
                    Percent = 0.15D,
                    Annum = true,
                    Accuracy = 0.314M
                },
                Some = SomeEnum.B,
                Start = DateTime.UtcNow,
                Duration = TimeSpan.Parse("-01:15:27.123"),
                Offset = DateTimeOffset.UtcNow,
                Parent = new FirstSerializableMessage
                {
                    Age = 10,
                    Address = Guid.NewGuid().ToString(),
                    Int = 7,
                    Name = "-1",
                    Risk = new Risk
                    {
                        Percent = 0.15D,
                        Annum = true,
                        Accuracy = 0.314M
                    }
                },
                Names = []
            };

            for (var i = 0; i < number; i++)
            {
                var firstMessage = new FirstSerializableMessage();
                secondMessage.Names.Add(firstMessage);
                firstMessage.Age = 10;
                firstMessage.Address = Guid.NewGuid().ToString();
                firstMessage.Int = 7;
                firstMessage.Name = i.ToString();
                firstMessage.Risk = new Risk
                {
                    Percent = 0.15D,
                    Annum = true,
                    Accuracy = 0.314M
                };
            }

            secondMessage.MoreNames = secondMessage.Names.ToArray();

            return secondMessage;
        }

        void Time(object message, XmlMessageSerializer serializer)
        {
            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(message, stream);
                }
            }

            watch.Stop();
            Debug.WriteLine("Serializing: " + watch.Elapsed);

            watch.Reset();

            byte[] buffer;
            using (var s = new MemoryStream())
            {
                serializer.Serialize(message, s);
                buffer = s.ToArray();
            }

            watch.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var forDeserializing = new MemoryStream(buffer))
                {
                    serializer.Deserialize(forDeserializing.ToArray());
                }
            }

            watch.Stop();
            Debug.WriteLine("Deserializing: " + watch.Elapsed);
        }


        [Test]
        public void NestedObjectWithNullPropertiesShouldBeSerialized()
        {
            var result = ExecuteSerializer.ForMessage<MessageWithNestedObject>(m => { m.NestedObject = new MessageWithNullProperty(); });
            Assert.That(result.NestedObject, Is.Not.Null);
        }

        [Test]
        public void Messages_with_generic_properties_closing_nullables_should_be_supported()
        {
            var theTime = DateTime.UtcNow;

            var result = ExecuteSerializer.ForMessage<MessageWithGenericPropClosingNullable>(
                m =>
                {
                    m.GenericNullable = new GenericPropertyWithNullable<DateTime?>
                    {
                        TheType = theTime
                    };
                    m.Whatever = "fdsfsdfsd";
                });
            Assert.That(result.GenericNullable.TheType, Is.EqualTo(theTime));
        }

        [Test]
        public void When_Using_A_Dictionary_With_An_object_As_Key_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => SerializerFactory.Create<MessageWithDictionaryWithAnObjectAsKey>());
        }

        [Test]
        public void When_Using_A_Dictionary_With_An_Object_As_Value_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => SerializerFactory.Create<MessageWithDictionaryWithAnObjectAsValue>());
        }


        [Test]
        [Ignore("We're not supporting this type")]
        public void System_classes_with_non_default_constructors_should_be_supported()
        {
            var message = new MailMessage("from@gmail.com", "to@hotmail.com")
            {
                Subject = "Testing the NSB email support",
                Body = "Hello"
            };

            var result = ExecuteSerializer.ForMessage<MessageWithSystemClassAsProperty>(
                m => { m.MailMessage = message; });
            Assert.That(result.MailMessage, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.MailMessage.From.Address, Is.EqualTo("from@gmail.com"));
                Assert.That(result.MailMessage.To.First(), Is.EqualTo(message.To.First()));
                Assert.That(result.MailMessage.BodyEncoding.CodePage, Is.EqualTo(message.BodyEncoding.CodePage));
                Assert.That(result.MailMessage.BodyEncoding.EncoderFallback.MaxCharCount, Is.EqualTo(message.BodyEncoding.EncoderFallback.MaxCharCount));
            });
        }

        [Test]
        [Ignore("We're currently not supporting polymorphic properties")]
        public void Messages_with_polymorphic_properties_should_be_supported()
        {
            var message = new PolyMessage
            {
                BaseType = new ChildOfBase
                {
                    BaseTypeProp = "base",
                    ChildProp = "Child"
                }
            };

            var result = ExecuteSerializer.ForMessage<PolyMessage>(message);

            Assert.Multiple(() =>
            {
                Assert.That(result.BaseType.BaseTypeProp, Is.EqualTo(message.BaseType.BaseTypeProp));

                Assert.That(((ChildOfBase)result.BaseType).ChildProp, Is.EqualTo(((ChildOfBase)message.BaseType).ChildProp));
            });
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xml()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" ></SomeProperty></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument
            {
                Document = XDocument.Load(new StringReader(XmlDocument))
            };
            var messageWithXElement = new MessageWithXElement
            {
                Document = XElement.Load(new StringReader(XmlElement))
            };

            var resultXDocument = ExecuteSerializer.ForMessage<MessageWithXDocument>(messageWithXDocument);
            var resultXElement = ExecuteSerializer.ForMessage<MessageWithXElement>(messageWithXElement);

            Assert.Multiple(() =>
            {
                Assert.That(resultXDocument.Document.ToString(), Is.EqualTo(messageWithXDocument.Document.ToString()));
                Assert.That(resultXElement.Document.ToString(), Is.EqualTo(messageWithXElement.Document.ToString()));
            });
        }

        [Test]
        public void Should_be_able_to_deserialize_many_messages_of_same_type()
        {
            var xml = @"<?xml version=""1.0"" ?>
<Messages xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">
    <EmptyMessage>
    </EmptyMessage>
    <EmptyMessage>
    </EmptyMessage>
    <EmptyMessage>
    </EmptyMessage>
</Messages>
";
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(xml);
                streamWriter.Flush();
                stream.Position = 0;
                var serializer = SerializerFactory.Create<EmptyMessage>();
                var msgArray = serializer.Deserialize(stream.ToArray(), new[]
                {
                    typeof(EmptyMessage)
                });
                Assert.That(msgArray, Has.Length.EqualTo(3));
            }
        }

        [Test]
        public void Object_property_with_primitive_or_struct_value_should_serialize_correctly()
        {
            // this fixes issue #2796
            var serializer = SerializerFactory.Create<SerializedPair>();
            var message = new SerializedPair
            {
                Key = "AddressId",
                Value = new Guid("{ebdeeb33-baa7-4100-b1aa-eb4d6816fd3d}")
            };

            object[] messageDeserialized;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(message, stream);

                stream.Position = 0;

                messageDeserialized = serializer.Deserialize(stream.ToArray(), new[]
                {
                    message.GetType()
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(((SerializedPair)messageDeserialized[0]).Key, Is.EqualTo(message.Key));
                Assert.That(((SerializedPair)messageDeserialized[0]).Value, Is.EqualTo(message.Value));
            });
        }

        [Test]
        public void Should_throw_exception_when_deserializing_payloads_with_nsb_types()
        {
            //Adding a reference to ReplyOptions just to re-enforce the fact this test relies on it indirectly through the xml being deserialized
            var nServiceBusPublicTypeName = nameof(ReplyOptions);

            var xml = $@"<?xml version=""1.0""?>
                        <{nServiceBusPublicTypeName} xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                                xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus"">
                        </{nServiceBusPublicTypeName}>";

            var serializer = SerializerInstanceWithZeroMessageTypes();
            Assert.Throws<Exception>(() => serializer.Deserialize(StringToByteArray(xml)));
        }

        [Test]
        public void Should_throw_exception_when_deserializing_payloads_with_system_types()
        {
            var xml = @"<?xml version=""1.0""?>
                        <ArrayList xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                        xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                        xmlns=""http://tempuri.net/System.Collections"">
                    </ArrayList>";

            var serializer = SerializerInstanceWithZeroMessageTypes();
            Assert.Throws<Exception>(() => serializer.Deserialize(StringToByteArray(xml)));
        }

        static XmlMessageSerializer SerializerInstanceWithZeroMessageTypes() => SerializerFactory.Create();

        static byte[] StringToByteArray(string input)
        {
            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream))
            {
                streamWriter.Write(input);
                streamWriter.Flush();
                stream.Position = 0;

                return stream.ToArray();
            }
        }

        int number = 1;
        int numberOfIterations = 100;
    }

    public class SerializedPair
    {
        public string Key { get; set; }
        public object Value { get; set; }
    }

    public class EmptyMessage : IMessage
    {
    }

    public struct StructMessage : IMessage
    {
        public string SomeProperty { get; set; }

        public string SomeField;
    }

    public class MessageWithStructProperty : IMessage
    {
        public SomeStruct StructProperty { get; set; }

        public struct SomeStruct
        {

        }
    }

    public class PolyMessage : IMessage
    {
        public BaseType BaseType { get; set; }
    }

    public class ChildOfBase : BaseType
    {
        public BaseType BaseType { get; set; }


        public string ChildProp { get; set; }
    }

    public class BaseType
    {
        public string BaseTypeProp { get; set; }
    }

    public class MessageWithGenericPropClosingNullable
    {
        public GenericPropertyWithNullable<DateTime?> GenericNullable { get; set; }
        public string Whatever { get; set; }
    }

    public class MessageWithNullProperty
    {
        public string WillBeNull { get; set; }
    }

    public class MessageWithDouble
    {
        public double Double { get; set; }
    }

    public class MessageWithGenericProperty
    {
        public GenericProperty<string> GenericProperty { get; set; }
        public GenericProperty<string> GenericPropertyThatIsNull { get; set; }
    }

    public class MessageWithNestedObject
    {
        public MessageWithNullProperty NestedObject { get; set; }
    }


    public class MessageWithSystemClassAsProperty
    {
        public MailMessage MailMessage { get; set; }
    }


    public class GenericPropertyWithNullable<T>
    {
        public T TheType { get; set; }
    }

    public class GenericProperty<T>
    {
        public GenericProperty(T value)
        {
            ReadOnlyBlob = value;
        }

        public T ReadOnlyBlob { get; }

        public string WhatEver { get; set; }
    }

    public class MessageWithDictionaryWithAnObjectAsKey
    {
#pragma warning disable PS0025 // Dictionary keys should implement IEquatable<T>
        public Dictionary<object, string> Content { get; set; }
#pragma warning restore PS0025 // Dictionary keys should implement IEquatable<T>
    }

    public class MessageWithDictionaryWithAnObjectAsValue
    {
        public Dictionary<string, object> Content { get; set; }
    }

    public class MessageWithListItem
    {
        public string Data { get; set; }
    }

    public class MessageWithInvalidCharacter : IMessage
    {
        public string Special { get; set; }
    }

    public class MessageWithList : IMessage
    {
        public List<MessageWithListItem> Items { get; set; }
    }


    public class MessageWithHashtable : IMessage
    {
        public Hashtable Hashtable { get; set; }
    }


    public class MessageWithArrayList : IMessage
    {
        public ArrayList ArrayList { get; set; }
    }

    public class MessageWithClosedListInAlternateNamespace : IMessage
    {
        public AlternateItemList Items { get; set; }
    }

    public class MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations : IMessage
    {
        public AlternateItemListMultipleIEnumerableImplementations Items { get; set; }
    }

    public class MessageWithClosedListInAlternateNamespaceMultipleIListImplementations : IMessage
    {
        public AlternateItemListMultipleIListImplementations Items { get; set; }
    }

    public class MessageWithClosedList : IMessage
    {
        public ItemList Items { get; set; }
    }


    public class MessageWithXDocument : IMessage
    {
        public XDocument Document { get; set; }
    }


    public class MessageWithXElement : IMessage
    {
        public XElement Document { get; set; }
    }

    public class ItemList : List<MessageWithListItem>
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
}

namespace NServiceBus.Serializers.XML.Test.AlternateNamespace
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    public class AlternateItemList : List<MessageWithListItemAlternate>
    {
    }

    public class MessageWithListItemAlternate
    {
        public string Data { get; set; }
    }

    public class AlternateItemListMultipleIEnumerableImplementations : List<MessageWithListItemAlternate>, IEnumerable<string>
    {
        public new IEnumerator<string> GetEnumerator()
        {
            return ToArray().Select(item => item.Data).GetEnumerator();
        }
    }

    public class AlternateItemListMultipleIListImplementations : List<MessageWithListItemAlternate>, IList<string>
    {
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return stringList.GetEnumerator();
        }

        void ICollection<string>.Add(string item)
        {
            stringList.Add(item);
        }

        bool ICollection<string>.Contains(string item)
        {
            return stringList.Contains(item);
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            stringList.CopyTo(array, arrayIndex);
        }

        bool ICollection<string>.Remove(string item)
        {
            return stringList.Remove(item);
        }

        bool ICollection<string>.IsReadOnly => stringList.IsReadOnly;

        int IList<string>.IndexOf(string item)
        {
            return stringList.IndexOf(item);
        }

        void IList<string>.Insert(int index, string item)
        {
            stringList.Insert(index, item);
        }

        string IList<string>.this[int index]
        {
            get { return stringList[index]; }
            set { stringList[index] = value; }
        }

        IList<string> stringList = [];
    }

    class MessageImplementingISerializable : ISerializable
    {
        public MessageImplementingISerializable(string readOnlyProperty)
        {
            ReadOnlyProperty = readOnlyProperty;
        }

        protected MessageImplementingISerializable(SerializationInfo info, StreamingContext context)
        {
            ReadOnlyProperty = info.GetString("ReadOnlyProperty");
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ReadOnlyProperty", ReadOnlyProperty);
        }

        public string ReadOnlyProperty { get; }
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
}
