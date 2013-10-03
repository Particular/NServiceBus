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
            var serializer = SerializerFactory.Create<MessageWithInvalidCharacter>();
            var msg = new MessageWithInvalidCharacter();

            var sb = new StringBuilder();
            sb.Append("Hello");
            sb.Append((char)0x1C);
            sb.Append("John");
            msg.Special = sb.ToString();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
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
            var serializer = SerializerFactory.Create<Command1>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] {new Command1(Guid.NewGuid()), new Command2(Guid.NewGuid())}, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);

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

                var msgArray = SerializerFactory.Create<MessageWithDouble>().Deserialize(stream);

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());

            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_element()
        {
            Serializer.ForMessage<EmptyMessage>(new EmptyMessage(), s =>
                { s.SkipWrappingElementForSingleMessages = true; })
                .AssertResultingXml(d=> d.DocumentElement.Name == "EmptyMessage","Root should be message typename");
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_xmlElement_raw_data()
        {
            var xmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";

            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(xmlElement)) };

            Serializer.ForMessage<MessageWithXElement>(messageWithXElement, s =>
            { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");
        }
        [Test]
        public void Should_be_able_to_serialize_single_message_without_wrapping_xmlDocument_raw_data()
        {
            var xmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>" ;

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(xmlDocument)) };

            Serializer.ForMessage<MessageWithXDocument>(messageWithXDocument, s =>
            { s.SkipWrappingRawXml = true; })
                .AssertResultingXml(d => d.DocumentElement.ChildNodes[0].FirstChild.Name != "Document", "Property name should not be available");

        }

        [Test]
        public void Should_be_able_to_deserialize_messages_which_xml_element_matches_property_name()
        {
            var xmlElement = "<Document xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></Document>";

            var messageWithXElement = new MessageWithXElement
                                      {
                                          Document = XElement.Load(new StringReader(xmlElement))
                                      };

            var serializer = SerializerFactory.Create<MessageWithXDocument>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { messageWithXElement }, stream);
                stream.Position = 0;

                var msg = serializer.Deserialize(stream).Cast<MessageWithXElement>().Single();

                Assert.NotNull(msg.Document);
                Assert.AreEqual("Document", msg.Document.Name.LocalName);
            }
        }

        [Test]
        public void Should_be_able_to_deserialize_messages_which_xml_document_matches_property_name()
        {
            var xmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><Document xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></Document>";

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(xmlDocument)) };

            var serializer = SerializerFactory.Create<MessageWithXDocument>();
            serializer.SkipWrappingRawXml = true;

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { messageWithXDocument }, stream);
                stream.Position = 0;
                var msg = serializer.Deserialize(stream).Cast<MessageWithXDocument>().Single();

                Assert.NotNull(msg.Document);
                Assert.AreEqual("Document", msg.Document.Root.Name.LocalName);
            }

            serializer = SerializerFactory.Create<MessageWithXElement>();
            serializer.SkipWrappingRawXml = true;

        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_default_namespaces_and_then_deserialize()
        {
            var serializer = SerializerFactory.Create<MessageWithDouble>();
            serializer.SkipWrappingElementForSingleMessages = true;
            var msg = new MessageWithDouble();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());
            }
        }

        [Test]
        public void Should_be_able_to_serialize_single_message_with_default_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.SkipWrappingElementForSingleMessages = true;
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://tempuri.net/NServiceBus.Serializers.XML.Test"">";

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
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
        public void Should_be_able_to_serialize_single_message_with_specified_namespaces()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();
            serializer.SkipWrappingElementForSingleMessages = true;
            serializer.Namespace = "http://super.com";
            var msg = new EmptyMessage();

            var expected = @"<EmptyMessage xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://super.com/NServiceBus.Serializers.XML.Test"">";
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
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

                var msgArray = SerializerFactory.Create<MessageWithDouble>().Deserialize(stream, new[] { typeof(MessageWithDouble) });

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

                Assert.AreEqual(typeof(MessageWithDouble), msgArray[0].GetType());
                Assert.AreEqual(typeof(EmptyMessage), msgArray[1].GetType());

            }
        }

        [Test]
        public void TestMultipleInterfacesDuplicatedProperty()
        {
            IMessageMapper mapper = new MessageMapper();
            var types = new List<Type> {typeof (IThird)};
            mapper.Initialize(types);
            var serializer = new XmlMessageSerializer(mapper);
            serializer.Initialize(types);
            var msgBeforeSerialization = mapper.CreateInstance<IThird>(x => x.FirstName = "Danny");

            var count = 0;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msgBeforeSerialization }, stream);
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
        public void Multiple_generic_properties_should_be_supported()
        {
            var result = ExecuteSerializer.ForMessage<MessageWithMultiGenericProperty>(m =>
                                                                         {
                                                                             m.GenericProperty =
                                                                                 new GenericProperty<string,int>
                                                                                 { 
                                                                                     KProperty = 6,
                                                                                     TProperty   = "foo"};
                                                                         });

            Assert.AreEqual(6, result.GenericProperty.KProperty);
            Assert.AreEqual("foo", result.GenericProperty.TProperty);
        }


        [Test]
        public void Culture()
        {
            var serializer = SerializerFactory.Create<MessageWithDouble>();
            var val = 65.36;
            var msg = new MessageWithDouble { Double = val };

            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var stream = new MemoryStream();
            serializer.Serialize(new object[] { msg }, stream);

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
            var types = new List<Type> {typeof (IM2)};
            var mapper = new MessageMapper();
            mapper.Initialize(types);
            var serializer = new XmlMessageSerializer(mapper);

            serializer.Initialize(types);

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

            o.ArrayFoos = new[] { new Foo { Name = "FooArray1", Title = "Mr." }, new Foo { Name = "FooArray2", Title = "Mrs" } };
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

            object[] messages = { o };

            Time(messages, serializer);
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
            {
                using (var stream = new MemoryStream())
                {
                    DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, stream);
                }
            }

            Debug.WriteLine("serialization " + sw.Elapsed);

            sw.Reset();

            File.Delete("a.xml");
            using (var fs = File.Open("a.xml", FileMode.OpenOrCreate))
                DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, fs);

            byte[] buffer;
            using (var s = new MemoryStream())
            {
                DataContractSerialize(xmlWriterSettings, dataContractSerializer, messages, s);
                buffer = s.GetBuffer();
            }

            sw.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var reader = XmlReader.Create(new MemoryStream(buffer), xmlReaderSettings))
                {
                    dataContractSerializer.ReadObject(reader);
                }
            }

            Debug.WriteLine("deserializing: " + sw.Elapsed);
        }

        [Test]
        public void SerializeLists()
        {
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = new MessageWithList
                      {
                          Items = new List<MessageWithListItem>
                                  {
                                      new MessageWithListItem
                                      {
                                          Data = "Hello"
                                      }
                                  }
                      };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = (MessageWithList)msgArray[0];
                Assert.AreEqual("Hello", m.Items.First().Data);
            }
        }

		[Test]
		public void SerializeClosedGenericListsInAlternateNamespace()
		{
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespace>();
            var msg = new MessageWithClosedListInAlternateNamespace
                      {
                          Items = new AlternateItemList
                                  {
                                      new MessageWithListItemAlternate
                                      {
                                          Data = "Hello"
                                      }
                                  }
                      };

		    using (var stream = new MemoryStream())
			{
				serializer.Serialize(new object[] { msg }, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespace)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First().Data);
			}
		}

        [Test]
		public void SerializeClosedGenericListsInAlternateNamespaceMultipleIEnumerableImplementations()
		{
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations>();
            var msg = new MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations
                      {
                          Items = new AlternateItemListMultipleIEnumerableImplementations
                                  {
                                      new MessageWithListItemAlternate
                                      {
                                          Data = "Hello"
                                      }
                                  }
                      };

            using (var stream = new MemoryStream())
			{
				serializer.Serialize(new object[] { msg }, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespaceMultipleIEnumerableImplementations)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First<MessageWithListItemAlternate>().Data);
			}
		}

        [Test]
		public void SerializeClosedGenericListsInAlternateNamespaceMultipleIListImplementations()
		{
			var serializer = SerializerFactory.Create<MessageWithClosedListInAlternateNamespaceMultipleIListImplementations>();
			var msg = new MessageWithClosedListInAlternateNamespaceMultipleIListImplementations
			          {
			              Items = new AlternateItemListMultipleIListImplementations
			                      {
			                          new MessageWithListItemAlternate
			                          {
			                              Data = "Hello"
			                          }
			                      }
			          };

            using (var stream = new MemoryStream())
			{
				serializer.Serialize(new object[] { msg }, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedListInAlternateNamespaceMultipleIListImplementations)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First<MessageWithListItemAlternate>().Data);
			}
		}

		[Test]
		public void SerializeClosedGenericListsInSameNamespace()
		{
			var serializer = SerializerFactory.Create<MessageWithClosedList>();
			var msg = new MessageWithClosedList
			          {
			              Items = new ItemList
			                      {
			                          new MessageWithListItem
			                          {
			                              Data = "Hello"
			                          }
			                      }
			          };

		    using (var stream = new MemoryStream())
			{
				serializer.Serialize(new object[] { msg }, stream);
				stream.Position = 0;

				var msgArray = serializer.Deserialize(stream);
				var m = (MessageWithClosedList)msgArray[0];
				Assert.AreEqual("Hello", m.Items.First().Data);
			}
		}

        [Test]
        public void SerializeEmptyLists()
        {
            var serializer = SerializerFactory.Create<MessageWithList>();
            var msg = new MessageWithList
                      {
                          Items = new List<MessageWithListItem>()
                      };

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { msg }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = (MessageWithList)msgArray[0];
                Assert.IsEmpty(m.Items);
            }
        }


        void DataContractSerialize(XmlWriterSettings xmlWriterSettings, DataContractSerializer dataContractSerializer, IMessage[] messages, Stream stream)
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
                                     Name = "udi",
                                     Age = 10,
                                     Address = Guid.NewGuid().ToString(),
                                     Int = 7
                                 }
                    };

            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };

            o.Names = new List<M1>();
            for (var i = 0; i < number; i++)
            {
                var m1 = new M1();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };
            }

            o.MoreNames = o.Names.ToArray();

            return o;
        }

        void Time(object[] messages, IMessageSerializer serializer)
        {
            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(messages, stream);
                }
            }

            Debug.WriteLine("Serializing: " + watch.Elapsed);

            watch.Reset();

            byte[] buffer;
            using (var s = new MemoryStream())
            {
                serializer.Serialize(messages, s);
                buffer = s.GetBuffer();
            }

            watch.Start();

            for (var i = 0; i < numberOfIterations; i++)
            {
                using (var forDeserializing = new MemoryStream(buffer))
                {
                    serializer.Deserialize(forDeserializing);
                }
            }

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
                    m.Whatever = "aString";
                });
            Assert.IsNotNull(result.GenericNullable.TheType == theTime);
        }

        [Test]
        public void When_Using_A_Dictionary_With_An_object_As_Key_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => ExecuteSerializer.ForMessage<MessageWithDictionaryWithAnObjectAsKey>(m => { }));
        }

        [Test]
        public void When_Using_A_Dictionary_With_An_Object_As_Value_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => ExecuteSerializer.ForMessage<MessageWithDictionaryWithAnObjectAsValue>(m =>{}));
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
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xmlElement()
        {
            var xmlElement = "<SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";

            var messageWithXElement = new MessageWithXElement { Document = XElement.Load(new StringReader(xmlElement)) };

            var resultXElement = ExecuteSerializer.ForMessage<MessageWithXElement>(messageWithXElement);

            Assert.AreEqual(messageWithXElement.Document.ToString(), resultXElement.Document.ToString());
        }

        [Test]
        public void When_Using_Property_WithXContainerAssignable_should_preserve_xmlDocument()
        {
            var xmlDocument = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><SomeClass xmlns=\"http://nservicebus.com\"><SomeProperty value=\"Bar\" /></SomeClass>";

            var messageWithXDocument = new MessageWithXDocument { Document = XDocument.Load(new StringReader(xmlDocument)) };

            var resultXDocument = ExecuteSerializer.ForMessage<MessageWithXDocument>(messageWithXDocument);
            Assert.AreEqual(messageWithXDocument.Document.ToString(), resultXDocument.Document.ToString());
        }

        [Test]
        public void Should_be_able_to_deserialize_many_messages_of_same_type()
        {
            var serializer = SerializerFactory.Create<EmptyMessage>();

            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new object[] { new EmptyMessage(), new EmptyMessage(), new EmptyMessage() }, stream);
                stream.Position = 0;

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
    public class MessageWithMultiGenericProperty
    {
        public GenericProperty<string,int> GenericProperty { get; set; }

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
    public class GenericProperty<T,K>
    {
        public T TProperty { get; set; }
        public K KProperty { get; set; }
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
		public IEnumerator<string> GetEnumerator()
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

    public class MessageWithInvalidCharacter : IMessage
    {
        public string Special { get; set; }
    }
}
