namespace NServiceBus.Core.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using MessageInterfaces.MessageMapper.Reflection;
    using NUnit.Framework;

    [TestFixture]
	public class When_mapping_iEnumerable_implementations
	{

		[Test]
		public void Class_implementing_iEnumerable_string_should_be_mapped()
        {
		    var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<string>)));
		}

		[Test]
		public void Class_implementing_iEnumerable_string_and_iReturnMyself_should_be_mapped()
        {
            var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerableAndIReturnMyself<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerableAndIReturnMyself<string>)));
		}

		[Test]
		public void Class_implementing_iEnumerable_returnMyself_should_be_mapped()
        {
            var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<ReturnMyself>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<ReturnMyself>)));
		}

		[Test]
		public void Class_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionObject) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionObject)));
		}

		[Test]
		public void Class_implementing_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself)));
		}

		[Test]
		public void Class_implementing_base_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
        {
            var mapper = new MessageMapper();
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself)));
		}

		internal class ClassImplementingIEnumerable<TItem> : IEnumerable<TItem>
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

		internal class ClassImplementingIEnumerableAndIReturnMyself<TItem> : IEnumerable<TItem>, IReturnMyself<ClassImplementingIEnumerableAndIReturnMyself<TItem>>
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

		internal interface IReturnMyself<T>
		{
			T ReturnMe();
		}

		internal class ReturnMyself : IReturnMyself<ReturnMyself>
		{
			public ReturnMyself ReturnMe()
			{
				return this;
			}
		}

		internal class DerivedReturnMyselfCollectionImplementingIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<DerivedReturnMyselfCollectionImplementingIReturnMyself>
		{
			public DerivedReturnMyselfCollectionImplementingIReturnMyself ReturnMe()
			{
				return this;
			}
		}

		internal class DerivedReturnMyselfCollectionObject : ClassImplementingIEnumerable<ReturnMyself>
		{
		}

		internal class DerivedReturnMyselfCollectionImplementingBaseIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<ClassImplementingIEnumerable<ReturnMyself>>
		{
			public ClassImplementingIEnumerable<ReturnMyself> ReturnMe()
			{
				return this;
			}
		}
	}
}
