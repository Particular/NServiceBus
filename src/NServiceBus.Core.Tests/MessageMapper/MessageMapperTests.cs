namespace MessageMapperTests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class MessageMapperTests
    {
        [Test]
        public void Initialize_ShouldBeThreadsafe()
        {
            var mapper = new MessageMapper();

            Parallel.For(0, 10, i =>
            {
                mapper.Initialize(new[]
                {
                    typeof(SampleMessageClass),
                    typeof(ISampleMessageInterface),
                    typeof(ClassImplementingIEnumerable<>)
                });
            });
        }

        [Test]
        public void CreateInstance_WhenMessageInitialized_ShouldBeThreadsafe()
        {
            var mapper = new MessageMapper();

            mapper.Initialize(new[]
                {
                    typeof(SampleMessageClass),
                    typeof(ISampleMessageInterface),
                    typeof(ClassImplementingIEnumerable<>)
                });

            Parallel.For(0, 10, i =>
            {
                mapper.CreateInstance<SampleMessageClass>();
                mapper.CreateInstance<ISampleMessageInterface>();
                mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
            });
        }

        [Test]
        public void CreateInstance_WhenMessageNotInitialized_ShouldBeThreadsafe()
        {
            var mapper = new MessageMapper();

            Parallel.For(0, 10, i =>
            {
                mapper.CreateInstance<SampleMessageClass>();
                mapper.CreateInstance<ISampleMessageInterface>();
                mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
            });
        }

        [Test]
        public void ShouldAllowMutlipleMapperInstancesPerAppDomain()
        {
            Parallel.For(0, 10, i =>
            {
                var mapper = new MessageMapper();
                mapper.CreateInstance<SampleMessageClass>();
                mapper.CreateInstance<ISampleMessageInterface>();
                mapper.CreateInstance<ClassImplementingIEnumerable<string>>();
            });
        }

        public class SampleMessageClass
        {
        }

        public interface ISampleMessageInterface 
        {
        }

        public class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
        {
            public IEnumerator<TItem> GetEnumerator()
            {
                return new List<TItem>.Enumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}