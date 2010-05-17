using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using System.Collections;
using NUnit.Framework;

namespace NServiceBus.Serializers.XML.Test
{
    [TestFixture]
    public class SerializerTests
    {
        private int number = 1;
        private int numberOfIterations = 100;


        [Test]
        public void Generic_properties_should_be_supported()
        {
            IMessageMapper mapper = new MessageMapper();
            var serializer = new MessageSerializer
                                 {
                                     MessageMapper = mapper,
                                     MessageTypes = new List<Type>(new[] {typeof (MessageWithGenericProperty)})
                                 };

            using (var stream = new MemoryStream())
            {
                var message = new MessageWithGenericProperty
                                  {
                                      GenericProperty = new GenericProperty<string>("test"){WhatEver = "a property"}
                                  };

                serializer.Serialize(new IMessage[] { message }, stream);

                stream.Position = 0;

                Debug.WriteLine(new StreamReader(stream).ReadToEnd());

                stream.Position = 0;

                var result = serializer.Deserialize(stream)[0] as MessageWithGenericProperty;

                Assert.NotNull(result);

                Assert.AreEqual(message.GenericProperty.WhatEver,result.GenericProperty.WhatEver);
            }
                
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
            var serializer = new MessageSerializer();
            serializer.MessageMapper = mapper;

            serializer.MessageTypes = new List<Type>(new[] {typeof(IM2)});

            var o = mapper.CreateInstance<IM2>();

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
            o.Lookup = new MyDic();
            o.Lookup["1"] = "1";
            o.Foos = new Dictionary<string, List<Foo>>();
            o.Foos["foo1"] = new List<Foo>(new[] { new Foo { Name="1", Title = "1"}, new Foo { Name = "2", Title = "2"}});

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

            IMessage[] result = null;

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
            catch(Exception e)
            {
                string s = e.Message;
            }
        }
    }

    public class MessageWithGenericProperty:IMessage
    {
        public GenericProperty<string> GenericProperty { get; set; }
    }

    public class GenericProperty<T>
    {
        private T value;

        public GenericProperty(T value)
        {
            this.value = value;
        }

        public T ReadOnlyBlob {
            get
            {
                return value;
            }
        }

        public string WhatEver { get; set; }
    }
}
