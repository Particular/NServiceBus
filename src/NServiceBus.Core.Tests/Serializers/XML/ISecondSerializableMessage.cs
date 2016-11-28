namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using System.Collections.Generic;

    public interface ISecondSerializableMessage : IFirstSerializableMessage
    {
        Guid Id { get; set; }
        IFirstSerializableMessage Parent { get; set; }
        List<IFirstSerializableMessage> Names { get; set; }
        SomeEnum Some { get; set; }
        DateTime Start { get; set; }
        TimeSpan Duration { get; set; }
        DateTimeOffset Offset { get; set; }
        IFirstSerializableMessage[] MoreNames { get; set; }
        MyDictionary Lookup { get; set; }
        Dictionary<string, List<Foo>> Foos { get; set; }
        byte[] Data { get; set; }
        IEnumerable<string> SomeStrings { get; set; }
        Foo[] ArrayFoos { get; set; }
        Bar[] Bars { get; set; }
        HashSet<int> NaturalNumbers { get; set; }
        HashSet<string> Developers { get; set; }
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
