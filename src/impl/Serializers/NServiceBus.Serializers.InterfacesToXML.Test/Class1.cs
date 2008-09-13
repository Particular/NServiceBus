using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NServiceBus.Serializers.InterfacesToXML.Test
{
    public class Class1
    {
        public void Test()
        {
            MessageSerializer serializer = new MessageSerializer();
            serializer.Initialize(typeof(IM2), typeof(IM1));

            IM2 o = serializer.CreateImplementationOf<IM2>();
            o.Age = 10;
            o.Address = Guid.NewGuid().ToString();
            o.Int = 7;
            o.Name = "udi";
            o.Risk = new Risk(0.15D, true);

            o.Parent = serializer.CreateImplementationOf<IM1>();
            o.Parent.Name = "udi";
            o.Parent.Age = 10;
            o.Parent.Address = Guid.NewGuid().ToString();
            o.Parent.Int = 7;
            o.Parent.Name = "-1";
            o.Parent.Risk = new Risk(0.15D, true);

            o.Names = new List<IM1>(10);
            for (int i = 0; i < 100; i++)
            {
                IM1 m1 = serializer.CreateImplementationOf<IM1>();
                o.Names.Add(m1);
                m1.Age = 10;
                m1.Address = Guid.NewGuid().ToString();
                m1.Int = 7;
                m1.Name = i.ToString();
                m1.Risk = new Risk(0.15D, true);
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000; i++)
            {
                MemoryStream stream = new MemoryStream();
                serializer.Serialize(new IMessage[] { o }, stream);

                MemoryStream forDeserializing = new MemoryStream(stream.GetBuffer());
                stream.Close();

                object result = serializer.Deserialize(forDeserializing);
                forDeserializing.Close();
            }

            watch.Stop();
            Debug.WriteLine(watch.Elapsed);
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

    public interface IM2 : IMessage
    {
        int Age { get; set; }
        int Int { get; set; }
        string Name { get; set; }
        string Address { get; set; }
        Risk Risk { get; set; }
        IM1 Parent { get; set; }
        List<IM1> Names { get; set; }
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

}
