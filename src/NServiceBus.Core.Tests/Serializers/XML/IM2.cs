namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;

    public class IM2 : IM1
    {
        public Guid Id { get; set; }
        public IM1 Parent { get; set; }
        public List<IM1> Names { get; set; }
        public SomeEnum Some { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTimeOffset Offset { get; set; }
        public IM1[] MoreNames { get; set; }
        public MyDictionary Lookup { get; set; }
        public Dictionary<string, List<Foo>> Foos { get; set; }
        public byte[] Data { get; set; }
        public IEnumerable<string> SomeStrings { get; set; }
        public Foo[] ArrayFoos { get; set; }
        public Bar[] Bars { get; set; }
        public HashSet<int> NaturalNumbers { get; set; }
        public HashSet<string> Developers { get; set; }
    }

    public class Foo
    {
        public string Name { get; set; }
        public string Title { get; set; }
    }

    public class Bar
    {
        public string Name { get; set; }
        public int Length { get; set; }
    }

    public class MyDictionary : Dictionary<string, string>
    {

    }
}
