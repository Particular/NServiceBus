namespace MessageMapperTests
{
    using System.Collections;
    using System.Collections.Generic;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_mapping_iEnumerable_implementations
    {

        [Test]
        public void Class_implementing_iEnumerable_string_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(ClassImplementingIEnumerable<string>)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<string>)));
        }

        [Test]
        public void Class_implementing_iEnumerable_string_and_iReturnMyself_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(ClassImplementingIEnumerableAndIReturnMyself<string>)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerableAndIReturnMyself<string>)));
        }
        class ClassImplementingIEnumerableAndIReturnMyself<TItem> : IEnumerable<TItem>, IReturnMyself<ClassImplementingIEnumerableAndIReturnMyself<TItem>>
        {
            public IEnumerator<TItem> GetEnumerator()
            {
                return new List<TItem>.Enumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }


            public ClassImplementingIEnumerableAndIReturnMyself<TItem> ReturnMe()
            {
                return this;
            }
        }

        [Test]
        public void Class_implementing_iEnumerable_returnMyself_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(ClassImplementingIEnumerable<ReturnMyself>)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<ReturnMyself>)));
        }

        [Test]
        public void Class_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(DerivedReturnMyselfCollectionObject)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionObject)));
        }

        class DerivedReturnMyselfCollectionObject : ClassImplementingIEnumerable<ReturnMyself>
        {
        }

        [Test]
        public void Class_implementing_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself)));
        }

        class DerivedReturnMyselfCollectionImplementingIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<DerivedReturnMyselfCollectionImplementingIReturnMyself>
        {
            public DerivedReturnMyselfCollectionImplementingIReturnMyself ReturnMe()
            {
                return this;
            }
        }


        [Test]
        public void Class_implementing_base_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
            mapper.Initialize(new[]
                {
                    typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself)
                });

            Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself)));
        }

        class DerivedReturnMyselfCollectionImplementingBaseIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<ClassImplementingIEnumerable<ReturnMyself>>
        {
            public ClassImplementingIEnumerable<ReturnMyself> ReturnMe()
            {
                return this;
            }
        }

        class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
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


        internal interface IReturnMyself<T>
        {
            T ReturnMe();
        }

        class ReturnMyself : IReturnMyself<ReturnMyself>
        {
            public ReturnMyself ReturnMe()
            {
                return this;
            }
        }

    }
}