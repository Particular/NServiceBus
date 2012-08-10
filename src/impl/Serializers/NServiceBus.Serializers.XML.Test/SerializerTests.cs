using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using System.Collections;
using NUnit.Framework;

namespace NServiceBus.Serializers.XML.Test
{
    using System.Net.Mail;
    using A;
    using B;

    [TestFixture]
    public class SerializerTests
    {
        private int number = 1;
        private int numberOfIterations = 100;
        

        [Test, Ignore("Not supported")]
        public void Should_deserialize_arraylist()
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

        [Test, Ignore("Not supported")]
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
            using (var stream = new MemoryStream())
            {
                SerializerFactory.Create(typeof(Command1), typeof(Command2)).Serialize(new object[] { new Command1(Guid.NewGuid()), new Command2(Guid.NewGuid()) }, stream);
                stream.Position = 0;

                var msgArray = SerializerFactory.Create(typeof(Command1), typeof(Command2)).Deserialize(stream);

                Assert.AreEqual(typeof(Command1), msgArray[0].GetType());
                Assert.AreEqual(typeof(Command2), msgArray[1].GetType());

            }    
        }

        [Test]
        public void TestMultipleInterfacesDuplicatedPropery()
        {
            IMessageMapper mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<IThird>();
            var msgBeforeSerialization = mapper.CreateInstance<IThird>(x => x.FirstName = "Danny");

            var count = 0;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(new[] { msgBeforeSerialization }, stream);
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
            double val = 65.36;
            var msg = new MessageWithDouble { Double = val };

            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var stream = new MemoryStream();
            serializer.Serialize(new[] { msg }, stream);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            stream.Position = 0;
            var msgArray = serializer.Deserialize(stream);
            var m = msgArray[0] as MessageWithDouble;

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
            IMessageMapper mapper = new MessageMapper();
            var serializer = SerializerFactory.Create<IM2>();


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
            o.Lookup = new MyDic();
            o.Lookup["1"] = "1";
            o.Foos = new Dictionary<string, List<Foo>>();
            o.Foos["foo1"] = new List<Foo>(new[] { new Foo { Name = "1", Title = "1" }, new Foo { Name = "2", Title = "2" } });
            o.Data = new byte[] { 1, 2, 3, 4, 5, 4, 3, 2, 1 };
            o.SomeStrings = new List<string> { "a", "b", "c" };

            o.ArrayFoos = new Foo[] { new Foo { Name = "FooArray1", Title = "Mr." }, new Foo { Name = "FooAray2", Title = "Mrs" } };
            o.Bars = new Bar[] { new Bar { Name = "Bar1", Length = 1 }, new Bar { Name = "BAr2", Length = 5 } };
            o.NaturalNumbers = new HashSet<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            o.Developers = new HashSet<string>(new string[] { "Udi Dahan", "Andreas Ohlund", "Matt Burton", "Jonathan Oliver et al" });

            o.Parent = mapper.CreateInstance<IM1>();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };

            o.Names = new List<IM1>();
            for (int i = 0; i < number; i++)
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

            IMessage[] messages = new IMessage[] { o };

            Time(messages, serializer);
        }

        [Test]
        public void TestDataContractSerializer()
        {
            M2 o = CreateM2();
            IMessage[] messages = new IMessage[] { o };

            DataContractSerializer dcs = new DataContractSerializer(typeof(ArrayList), new Type[] { typeof(M2), typeof(SomeEnum), typeof(M1), typeof(Risk), typeof(List<M1>) });

            Stopwatch sw = new Stopwatch();
            sw.Start();

            XmlWriterSettings xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = false;

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.IgnoreProcessingInstructions = true;
            xrs.ValidationType = ValidationType.None;
            xrs.IgnoreWhitespace = true;
            xrs.CheckCharacters = false;
            xrs.ConformanceLevel = ConformanceLevel.Auto;

            for (int i = 0; i < numberOfIterations; i++)
                using (MemoryStream stream = new MemoryStream())
                    DataContractSerialize(xws, dcs, messages, stream);

            sw.Stop();
            Debug.WriteLine("serialization " + sw.Elapsed);

            sw.Reset();

            File.Delete("a.xml");
            using (FileStream fs = File.Open("a.xml", FileMode.OpenOrCreate))
                DataContractSerialize(xws, dcs, messages, fs);

            MemoryStream s = new MemoryStream();
            DataContractSerialize(xws, dcs, messages, s);
            byte[] buffer = s.GetBuffer();
            s.Dispose();

            sw.Start();

            for (int i = 0; i < numberOfIterations; i++)
                using (XmlReader reader = XmlReader.Create(new MemoryStream(buffer), xrs))
                    dcs.ReadObject(reader);

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
                serializer.Serialize(new[] { msg }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = msgArray[0] as MessageWithList;
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
                serializer.Serialize(new[] { msg }, stream);
                stream.Position = 0;

                var msgArray = serializer.Deserialize(stream);
                var m = msgArray[0] as MessageWithList;
                Assert.IsEmpty(m.Items);
            }
        }


        private void DataContractSerialize(XmlWriterSettings xws, DataContractSerializer dcs, IMessage[] messages, Stream str)
        {
            ArrayList o = new ArrayList(messages);
            using (XmlWriter xwr = XmlWriter.Create(str, xws))
            {
                dcs.WriteStartObject(xwr, o);
                dcs.WriteObjectContent(xwr, o);
                dcs.WriteEndObject(xwr);
            }
        }

        private M2 CreateM2()
        {
            M2 o = new M2();
            o.Id = Guid.NewGuid();
            o.Age = 10;
            o.Address = Guid.NewGuid().ToString();
            o.Int = 7;
            o.Name = "udi";
            o.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };
            o.Some = SomeEnum.B;
            o.Start = DateTime.Now;
            o.Duration = TimeSpan.Parse("-01:15:27.123");
            o.Offset = DateTimeOffset.Now;

            o.Parent = new M1();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk { Percent = 0.15D, Annum = true, Accuracy = 0.314M };

            o.Names = new List<M1>();
            for (int i = 0; i < number; i++)
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

        private void Time(IMessage[] messages, IMessageSerializer serializer)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < numberOfIterations; i++)
                using (MemoryStream stream = new MemoryStream())
                    serializer.Serialize(messages, stream);

            watch.Stop();
            Debug.WriteLine("Serializing: " + watch.Elapsed);

            watch.Reset();

            MemoryStream s = new MemoryStream();
            serializer.Serialize(messages, s);
            byte[] buffer = s.GetBuffer();
            s.Dispose();

            watch.Start();

            object[] result = null;

            for (int i = 0; i < numberOfIterations; i++)
                using (var forDeserializing = new MemoryStream(buffer))
                    result = serializer.Deserialize(forDeserializing);

            watch.Stop();
            Debug.WriteLine("Deserializing: " + watch.Elapsed);
        }

        public void TestSchemaValidation()
        {
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(null, "schema0.xsd");
                settings.Schemas.Add(null, "schema1.xsd");
                settings.ValidationType = ValidationType.Schema;
                XmlDocument document = new XmlDocument();
                document.Load("XMLFile1.xml");
                XmlReader rdr = XmlReader.Create(new StringReader(document.InnerXml), settings);
                while (rdr.Read()) { }
            }
            catch (Exception e)
            {
                string s = e.Message;
            }
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



        [Test,Ignore("We're not supporting this type")]
        public void System_classes_with_non_default_ctors_should_be_supported()
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
            Assert.AreEqual( "from@gmail.com",result.MailMessage.From.Address);
            Assert.AreEqual(message.To.First(), result.MailMessage.To.First());
            Assert.AreEqual(message.BodyEncoding.CodePage, result.MailMessage.BodyEncoding.CodePage);
            Assert.AreEqual(message.BodyEncoding.EncoderFallback.MaxCharCount, result.MailMessage.BodyEncoding.EncoderFallback.MaxCharCount);

        }

        [Test,Ignore("We're currently not supporting polymorphic properties")]
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
}
