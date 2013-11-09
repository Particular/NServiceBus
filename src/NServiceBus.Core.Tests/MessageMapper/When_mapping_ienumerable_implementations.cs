namespace NServiceBus.MessageInterfaces.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
	public class When_mapping_iEnumerable_implementations
	{
		IMessageMapper mapper;

		[SetUp]
		public void SetUp()
		{
			mapper = new MessageMapper.Reflection.MessageMapper();
		}

		[Test]
		public void Class_implementing_iEnumerable_string_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<string>)));
		}

		[Test]
		public void Class_implementing_iEnumerable_string_and_iReturnMyself_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerableAndIReturnMyself<string>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerableAndIReturnMyself<string>)));
		}

		[Test]
		public void Class_implementing_iEnumerable_returnMyself_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(ClassImplementingIEnumerable<ReturnMyself>) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(ClassImplementingIEnumerable<ReturnMyself>)));
		}

		[Test]
		public void Class_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionObject) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionObject)));
		}

		[Test]
		public void Class_implementing_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingIReturnMyself)));
		}

		[Test]
		public void Class_implementing_base_returnMyself_inheriting_from_iEnumerable_returnMyself_implementation_should_be_mapped()
		{
			mapper.Initialize(new[] { typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself) });

			Assert.NotNull(mapper.GetMappedTypeFor(typeof(DerivedReturnMyselfCollectionImplementingBaseIReturnMyself)));
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

		class DerivedReturnMyselfCollectionImplementingIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<DerivedReturnMyselfCollectionImplementingIReturnMyself>
		{
			public DerivedReturnMyselfCollectionImplementingIReturnMyself ReturnMe()
			{
				return this;
			}
		}

		class DerivedReturnMyselfCollectionObject : ClassImplementingIEnumerable<ReturnMyself>
		{
		}

		class DerivedReturnMyselfCollectionImplementingBaseIReturnMyself : ClassImplementingIEnumerable<ReturnMyself>, IReturnMyself<ClassImplementingIEnumerable<ReturnMyself>>
		{
			public ClassImplementingIEnumerable<ReturnMyself> ReturnMe()
			{
				return this;
			}
		}
	}
}
