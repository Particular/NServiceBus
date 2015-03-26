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
    using B;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;
    using Serialization;

    [TestFixture]
    public class SerializerTests
    {
        private int number = 1;
        private int numberOfIterations = 100;

        [Test]
        public void SerializeInvalidCharacters()
        {
            IMessageMapper mapper = new MessageMapper();
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

                var msgArray = serializer.Deserialize(stream);
                var m = (MessageWithInvalidCharacter)msgArray[0];
                Assert.AreEqual(sb.ToString(), m.Special);
            }
        }

        [Test, Ignore("ArrayList is not supported")]
        public void Should_deserialize_arrayList()
        {
            var expected = new ArrayList
                               {
                                   "Value1",
                                   "Value2",
                                   "Value3",
                               };
            var result = ExecuteSerializer.ForMessage<MessageWithArrayList>(m3 => m3.ArrayList = expected);

            CollectionAssert.AreEqual(expected, result.ArrayList);
        }

        [Test, Ignore("Hashtable is not supported")]
        public void Should_deserialize_hashtable()
        {
            var expected = new Hashtable
                               {
                                   {"Key1", "Value1"},
                                   {"Key2", "Value2"},
                                   {"Key3", "Value3"},
                               };
            var result = ExecuteSerializer.ForMessage<MessageWithHashtable>(m3 => m3.Hashtable = expected);

            CollectionAssert.AreEqual(expected, result.Hashtable);
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
                var msgArray = SerializerFactory.Create(typeof(Command1), typeof(Command2)).Deserialize(stream);

                Assert.AreEqual(typeof(Command1), msgArray[0].GetType());
                Assert.AreEqual(typeof(Command2), msgArray[1].GetType());
            }
        }

        [Test]
        public void Should_deserialize_a_single_message_where_root_element_is_the_typeName()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.WriteLine("<NServiceBus.Serializers.XML.Test.MessageWithDouble><Double>23.4</Double></NServiceBus.Serializers.XML.Test.MessageWithDouble>");
                writer.Flush();
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream);

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());

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

                var result = deserializer.Deserialize(stream, new[] { typeof(IMyEventA), typeof(IMyEventB) });
                var a = (IMyEventA)result[0];
                var b = (IMyEventB)result[1];
                Assert.AreEqual(42, b.IntValue);
                Assert.AreEqual("Answer", a.StringValue);
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_element()
        {
            Serializer.ForMessage<EmptyMessage>(new EmptyMessage())
                .AssertResultingXml(d=> d.DocumentElement.Name == "EmptyMessage","Root should be message typename");
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_xml_raw_data()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            Serializer.ForMessage<MessageWithXDocument>(messageWithXDocument, s =>
            { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");

            Serializer.ForMessage<MessageWithXElement>(messageWithXElement, s =>
            { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");
        }

        [Test]
        public void Should_be_able_to_deserialize_messages_which_xml_raw_data_root_element_matches_property_name()
        {
            const string XmlElement = "<Document xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></Document>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            var serializer = SerializerFactory.Create<MessageWithXDocument>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXDocument, stream);
                stream.Position = 0;

                serializer = SerializerFactory.Create(typeof (MessageWithXDocument));
                serializer.SkipWrappingRawXml = true;

                var msg = serializer.Deserialize(stream).Cast<MessageWithXDocument>().Single();

                Assert.NotNull(msg.Document);
                Assert.AreEqual("Document", msg.Document.Root.Name.LocalName);
            }

            serializer = SerializerFactory.Create<MessageWithXElement>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(messageWithXElement, stream);
                stream.Position = 0;

                serializer = SerializerFactory.Create(typeof (MessageWithXElement));
                serializer.SkipWrappingRawXml = true;

                var msg = serializer.Deserialize(stream).Cast<MessageWithXElement>().Single();

                Assert.NotNull(msg.Document);
                Assert.AreEqual("Document", msg.Document.Name.LocalName);
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

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream);

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_default_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">";

            AssertSerializedEquals(serializer, msg, expected);
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_specified_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.Namespace = "http://super.com";
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://super.com/NServiceBus.Serializers.XML.Test"">";

            AssertSerializedEquals(serializer, msg, expected);
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_specified_namespace_with_trailing_forward_slashes()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.Namespace = "http://super.com///";
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://super.com/NServiceBus.Serializers.XML.Test"">";

            AssertSerializedEquals(serializer, msg, expected);
        }

        static void AssertSerializedEquals(IMessageSerializer serializer, IMessage msg, string expected)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                string result;
                using (var reader = new StreamReader(stream))
                {
                    reader.ReadLine();
                    result = reader.ReadLine();
                }

                Assert.AreEqual(expected, result);
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

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble)).Deserialize(stream, new[] { typeof(MessageWithDouble) });

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());

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

                var msgArray = SerializerFactory.Create(typeof(MessageWithDouble),typeof(EmptyMessage))
                    .Deserialize(stream, new[] { typeof(MessageWithDouble), typeof(EmptyMessage) });

                Assert.AreEqual(23.4, ((MessageWithDouble)msgArray[0]).Double);
                Assert.AreEqual(typeof(EmptyMessage), msgArray[1].GetType());

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
                    if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "FirstName"))
                        count++;
            }
            Assert.AreEqual(count, 1);
        }


        [Test]
        public void Generic_properties_should_be_supported()
        {

            var result = ExecuteSerializer.ForMessage<MessageWithGenericProperty>(m =>
                                                                         {
                                                                             m.GenericProperty =
                                                                                 new GenericProperty<string>("test") { WhatEver = "a property" };
                                                                         });

            Assert.AreEqual("a property", result.GenericProperty.WhatEver);
        }


        [Test]
        public void Culture()
        {
            var serializer = SerializerFactory.Create<MessageWithDouble>();
            var val = 65.36;
            var msg = new MessageWithDouble { Double = val };

            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var stream = new MemoryStream();
            serializer.Serialize(msg, stream);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            stream.Position = 0;
            var msgArray = serializer.Deserialize(stream);
            var m = (MessageWithDouble)msgArray[0];

            Assert.AreEqual(val, m.Double);

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
            var serializer = SerializerFactory.Create<IM2>(mapper);


            var o = mapper.CreateInstance<IM2>();

            o.Id = Guid.NewGuid();
            o.Age = 10;
            o.Address = Guid.NewGuid().ToString();
            o.Int = 7;
            o.Name = "udi";
            o.Uri = new Uri("http://www.UdiDahan.com/");
            o.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };
            o.Some = SomeEnum.B;
            o.Start = DateTime.Now;
            o.Duration = TimeSpan.Parse("-01:15:27.123");
            o.Offset = DateTimeOffset.Now;
            o.Lookup = new MyDictionary();
            o.Lookup["1"] = "1";
            o.Foos = new Dictionary<string, List<Foo>>();
            o.Foos["foo1"] = new List<Foo>(new[] { new Foo { Name = "1", Title = "1" }, new Foo { Name = "2", Title = "2" } });
            o.Data = new byte[] { 1, 2, 3, 4, 5, 4, 3, 2, 1 };
            o.SomeStrings = new List<string> { "a", "b", "c" };

            o.ArrayFoos = new[] { new Foo { Name = "FooArray1", Title = "Mr." }, new Foo { Name = "FooAray2", Title = "Mrs" } };
            o.Bars = new[] { new Bar { Name = "Bar1", Length = 1 }, new Bar { Name = "BAr2", Length = 5 } };
            o.NaturalNumbers = new HashSet<int>(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            o.Developers = new HashSet<string>(new[] { "Udi Dahan", "Andreas Ohlund", "Matt Burton", "Jonathan Oliver et al" });

            o.Parent = mapper.CreateInstance<IM1>();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };

            o.Names = new List<IM1>();
            for (var i = 0; i < number; i++)
            {
                var m1 = mapper.CreateInstance<IM1>();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };
            }

            o.MoreNames = o.Names.ToArray();

            Time(o, serializer);
        }

        [Test]
        public void TestDataContractSerializer()
        {
            var o = CreateM2();
            var messages = new IMessage[] { o };

            var dataContractSerializer = new DataContractSerializer(typeof(ArrayList), new[] { typeof(M2), typeof(SomeEnum), typeof(M1), typeof(Risk), typeof(List<M1>) });

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
                using (var stream = new MemoryStream())
                    DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, stream);

            sw.Stop();
            Debug.WriteLine("serialization " + sw.Elapsed);

            sw.Reset();

            File.Delete("a.xml");
            using (var fs = File.Open("a.xml", FileMode.OpenOrCreate))
                DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, fs);

            var s = new MemoryStream();
            DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, s);
            var buffer = s.GetBuffer();
            s.Dispose();

            sw.Start();

            for (var i = 0; i < numberOfIterations; i++)
                using (var reader = XmlReader.Create(new MemoryStream(buffer), xmlReaderSettings))
                    dataContractSerializer.ReadObject(reader);

            sw.Stop();
            Debug.WriteLine("deserializing: " + sw.Elapsed);
        }

        [Test]
        public void SerializeLists()
        {
            IMessageMapper mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = mapper.CreateInstance<MessageWithList>();

            msg.Items = new List<MessageWithListItem> { new MessageWithListItem { Data = "Hello" } };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = (MessageWithList)msgArray[0];
                Assert.AreEqual("Hello", m.Items.First().Data);
            }
        }

		[Test]
		public void SerializeClosedGenericListsInAlternateNamespace()
		{
			IMessageMapper mapper = new MessageMapper();
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespace>();
			var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespace>();

			msg.Items = new AlternateNamespace.AlternateItemList { new AlternateNamespace.MessageWithListItemAlternate { Data = "Hello" } };

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(msg, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespace)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First().Data);
			}
		}

        [Test]
		public void SerializeClosedGenericListsInAlternateNamespaceMultipleIEnumerableImplementations()
		{
			IMessageMapper mapper = new MessageMapper();
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations>();
			var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations>();

			msg.Items = new AlternateNamespace.AlternateItemListMultipleIEnumerableImplementations { new AlternateNamespace.MessageWithListItemAlternate { Data = "Hello" } };

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(msg, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First<AlternateNamespace.MessageWithListItemAlternate>().Data);
			}
		}

        [Test]
		public void SerializeClosedGenericListsInAlternateNamespaceMultipleIListImplementations()
		{
			IMessageMapper mapper = new MessageMapper();
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIListImplementations>();
			var msg = mapper.CreateInstance<MessageWithClosedListInAlternateNamespaceMultipleIListImplementations>();

			msg.Items = new AlternateNamespace.AlternateItemListMultipleIListImplementations { new AlternateNamespace.MessageWithListItemAlternate { Data = "Hello" } };

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(msg, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespaceMultipleIListImplementations)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First<AlternateNamespace.MessageWithListItemAlternate>().Data);
			}
		}

		[Test]
		public void SerializeClosedGenericListsInSameNamespace()
		{
			IMessageMapper mapper = new MessageMapper();
			var serializer = SerializerFactory.Create<MessageWithClosedList>();
			var msg = mapper.CreateInstance<MessageWithClosedList>();

			msg.Items = new ItemList { new MessageWithListItem { Data = "Hello" } };

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(msg, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedList)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First().Data);
			}
		}

        [Test]
        public void SerializeEmptyLists()
        {
            IMessageMapper mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = mapper.CreateInstance<MessageWithList>();

            msg.Items = new List<MessageWithListItem>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(msg, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = (MessageWithList)msgArray[0];
                Assert.IsEmpty(m.Items);
            }
        }


        private void DataContractSerialize(XmlWriterSettings xmlWriterSettings, DataContractSerializer dataContractSerializer, IMessage[] messages, Stream stream)
        {
            var o = new ArrayList(messages);
            using (var xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
            {
                dataContractSerializer.WriteStartObject(xmlWriter, o);
                dataContractSerializer.WriteObjectContent(xmlWriter, o);
                dataContractSerializer.WriteEndObject(xmlWriter);
            }
        }

        M2 CreateM2()
        {
            var o = new M2
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
                Start = DateTime.Now,
                Duration = TimeSpan.Parse("-01:15:27.123"),
                Offset = DateTimeOffset.Now,
                Parent = new M1
                {
                    Age = 10,
                    Address = Guid.NewGuid().ToString(), Int = 7,
                    Name = "-1",
                    Risk = new Risk
                    {
                        Percent = 0.15D,
                        Annum = true,
                        Accuracy = 0.314M
                    }
                },
                Names = new List<M1>()
            };

            for (var i = 0; i < number; i++)
            {
                var m1 = new M1();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk
                {
                    Percent = 0.15D,
                    Annum = true, 
                    Accuracy = 0.314M
                };
            }

            o.MoreNames = o.Names.ToArray();

            return o;
        }

        private void Time(object message, IMessageSerializer serializer)
        {
            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < numberOfIterations; i++)
                using (var stream = new MemoryStream())
                    serializer.Serialize(message, stream);

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
                    serializer.Deserialize(forDeserializing);
                }
            }

            watch.Stop();
            Debug.WriteLine("Deserializing: " + watch.Elapsed);
        }


        [Test]
        public void NestedObjectWithNullPropertiesShouldBeSerialized()
        {
            var result = ExecuteSerializer.ForMessage<MessageWithNestedObject>(m =>
            {
                m.NestedObject = new MessageWithNullProperty();
            });
            Assert.IsNotNull(result.NestedObject);
        }

        [Test]
        public void Messages_with_generic_properties_closing_nullables_should_be_supported()
        {
            var theTime = DateTime.Now;

            var result = ExecuteSerializer.ForMessage<MessageWithGenericPropClosingNullable>(
                m =>
                {
                    m.GenericNullable = new GenericPropertyWithNullable<DateTime?> { TheType = theTime };
                    m.Whatever = "fdsfsdfsd";
                });
            Assert.IsNotNull(result.GenericNullable.TheType == theTime);
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



        [Test, Ignore("We're not supporting this type")]
        public void System_classes_with_non_default_constructors_should_be_supported()
        {
            var message = new MailMessage("from@gmail.com", "to@hotmail.com")
                                                {
                                                    Subject = "Testing the NSB email support",
                                                    Body = "Hello",
                                                };

            var result = ExecuteSerializer.ForMessage<MessageWithSystemClassAsProperty>(
                    m =>
                    {
                        m.MailMessage = message;
                    });
            Assert.IsNotNull(result.MailMessage);
            Assert.AreEqual("from@gmail.com", result.MailMessage.From.Address);
            Assert.AreEqual(message.To.First(), result.MailMessage.To.First());
            Assert.AreEqual(message.BodyEncoding.CodePage, result.MailMessage.BodyEncoding.CodePage);
            Assert.AreEqual(message.BodyEncoding.EncoderFallback.MaxCharCount, result.MailMessage.BodyEncoding.EncoderFallback.MaxCharCount);

        }

        [Test, Ignore("We're currently not supporting polymorphic properties")]
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

            Assert.AreEqual(message.BaseType.BaseTypeProp, result.BaseType.BaseTypeProp);

            Assert.AreEqual(((ChildOfBase)message.BaseType).ChildProp, ((ChildOfBase)result.BaseType).ChildProp);
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xml()
        {
            const string XmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";
            const string XmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + XmlElement;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(XmlDocument)) };
            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(XmlElement)) };

            var resultXDocument = ExecuteSerializer.ForMessage<MessageWithXDocument>(messageWithXDocument);
            var resultXElement = ExecuteSerializer.ForMessage<MessageWithXElement>(messageWithXElement);

            Assert.AreEqual(messageWithXDocument.Document.ToString(), resultXDocument.Document.ToString());
            Assert.AreEqual(messageWithXElement.Document.ToString(), resultXElement.Document.ToString());
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
                var msgArray = serializer.Deserialize(stream, new[] { typeof(EmptyMessage) });
                Assert.AreEqual(3, msgArray.Length);
            }
        }
    }

    public class EmptyMessage:IMessage
    {
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
        private T value;

        public GenericProperty(T value)
        {
            this.value = value;
        }

        public T ReadOnlyBlob
        {
            get
            {
                return value;
            }
        }

        public string WhatEver { get; set; }
    }

    public class MessageWithDictionaryWithAnObjectAsKey
    {
        public Dictionary<object, string> Content { get; set; }
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

    [Serializable]
    public class MessageWithHashtable : IMessage
    {
        public Hashtable Hashtable { get; set; }
    }

    [Serializable]
    public class MessageWithArrayList : IMessage
    {
        public ArrayList ArrayList { get; set; }
    }

	public class MessageWithClosedListInAlternateNamespace : IMessage
	{
		public AlternateNamespace.AlternateItemList Items { get; set; }
	}

	public class MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations : IMessage
	{
		public AlternateNamespace.AlternateItemListMultipleIEnumerableImplementations Items { get; set; }
	}

	public class MessageWithClosedListInAlternateNamespaceMultipleIListImplementations : IMessage
	{
		public AlternateNamespace.AlternateItemListMultipleIListImplementations Items { get; set; }
	}

	public class MessageWithClosedList : IMessage
	{
		public ItemList Items { get; set; }
	}

    [Serializable]
    public class MessageWithXDocument : IMessage
    {
        public XDocument Document { get; set; }
    }

    [Serializable]
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
		private IList<string> stringList  = new List<string>();

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

		bool ICollection<string>.IsReadOnly
		{
			get { return stringList.IsReadOnly; }
		}

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
	}
}
