using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Serialization;
using System.Runtime.Serialization;
using System.Xml;
using System.Collections;

namespace NServiceBus.Serializers.InterfacesToXML.Test
{
    public class Class1
    {
        private int number = 1;
        private int numberOfIterations = 100;

        public void Test()
        {
            Debug.WriteLine("Interfaces");
            TestInterfaces();

            Debug.WriteLine("DataContractSerializer");
            TestDataContractSerializer();
        }

        public void TestInterfaces()
        {
            IMessageMapper mapper = new MessageMapper();
            MessageSerializer serializer = new MessageSerializer();
            serializer.MessageMapper = mapper;

            serializer.Initialize(typeof(IM2), typeof(IM1));

            IM2 o = mapper.CreateInstance<IM2>();

            o.Id = Guid.NewGuid();
            o.Age = 10;
            o.Address = Guid.NewGuid().ToString();
            o.Int = 7;
            o.Name = "udi";
            o.Risk = new Risk(0.15D, true);
            o.Some = SomeEnum.B;
            o.Start = DateTime.Now;
            o.Duration = TimeSpan.Parse("01:15:27.123");

            o.Parent = mapper.CreateInstance<IM1>();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk(0.15D, true);

            o.Names = new List<IM1>();
            for (int i = 0; i < number; i++)
            {
                IM1 m1 = mapper.CreateInstance<IM1>();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk(0.15D, true);
            }

            IMessage[] messages = new IMessage[] {o, o, o};

            Time(messages, serializer);
        }

        private void TestDataContractSerializer()
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
            o.Risk = new Risk(0.15D, true);
            o.Some = SomeEnum.B;

            o.Parent = new M1();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk(0.15D, true);

            o.Names = new List<M1>();
            for (int i = 0; i < number; i++)
            {
                M1 m1 = new M1();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk(0.15D, true);
            }

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

            for (int i = 0; i < numberOfIterations; i++)
                using (MemoryStream forDeserializing = new MemoryStream(buffer))
                    serializer.Deserialize(forDeserializing);

            watch.Stop();
            Debug.WriteLine("Deserializing: " + watch.Elapsed);
        }

    }

    public interface IM1 : IMessage
    {
        int Age { get; set; }
        int Int { get; set; }
        string Name { get; set; }
        string Address { get; set; }
        Risk Risk { get; set; }
    }

    public interface IM2 : IM1
    {
        Guid Id { get; set; }
        IM1 Parent { get; set; }
        List<IM1> Names { get; set; }
        SomeEnum Some { get; set; }
        DateTime Start { get; set; }
        TimeSpan Duration { get; set; }
        IM1[] MoreNames { get; set; }
    }

    [Serializable]
    public class M2 : M1
    {
        private Guid id;
        private List<M1> names;
        private M1 parent;
        private SomeEnum some;
        private DateTime start;
        private TimeSpan duration;

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public List<M1> Names
        {
            get { return names; }
            set { names = value; }
        }

        public M1 Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public SomeEnum Some
        {
            get { return some; }
            set { some = value; }
        }

        public DateTime Start
        {
            get { return start; }
            set { start = value; }
        }

        public TimeSpan Duration
        {
            get { return duration; }
            set { duration = value; }
        }
    }

    [Serializable]
    public class M1 : IMessage
    {
        private int age;
        private int intt;
        private string name;
        private string address;
        private Risk risk;

        public int Age
        {
            get { return age; }
            set { age = value; }
        }

        public int Int
        {
            get { return intt; }
            set { intt = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        public Risk Risk
        {
            get { return risk; }
            set { risk = value; }
        }
    }

    [Serializable]
    public class Risk
    {
        public Risk() { }
        public Risk(double percent, bool annnum)
        {
            this.percent = percent;
            this.annum = annnum;
        }

        private double percent;

        public bool Annum
        {
            get { return annum; }
            set { annum = value; }
        }

        public double Percent
        {
            get { return percent; }
            set { percent = value; }
        }

        private bool annum;
    }

    public enum SomeEnum
    {
        A,
        B
    }
}
